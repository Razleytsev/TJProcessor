using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Services;

public interface IOrderService
{
    Task<CodeOrder?> GetOrderByIdAsync(int id);
    Task<IEnumerable<CodeOrder>> GetOrdersAsync();
    Task<CodeOrder?> GetExternalOrderByIdAsync(int id);
    Task<CodeOrder?> ProcessCodeEmissionAsync(int id);
    Task<CodeOrder?> GetCodesFromOrderAsync(int id);
    Task<CodeOrder?> CreateOrderAsync(OrderCreateForm order);
    Task<CodeOrderContent?> DownloadOrderContentAsync(int id, string user);
}

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _externalEmission;
    private readonly ILogger<OrderService> _logger;
    private readonly IConfiguration _configuration;

    public OrderService(
        ApplicationDbContext context,
        IExternalEmission externalEmission,
        ILogger<OrderService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _externalEmission = externalEmission;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IEnumerable<CodeOrder>> GetOrdersAsync()
    {
        return await _context.CodeOrders.Include(order => order.Product).ToListAsync();
    }

    public async Task<CodeOrder?> GetOrderByIdAsync(int id)
    {
        var order = await _context.CodeOrders.FindAsync(id);
        if (order == null) return null;

        order.Content = await _context.CodeOrdersContents.FirstOrDefaultAsync(x => x.CodeOrderId == id);
        order.Product = await _context.Products.FirstOrDefaultAsync(x => x.Id == order.ProductId);
        return order;
    }

    public async Task<CodeOrder?> GetExternalOrderByIdAsync(int id)
    {
        var localOrder = await _context.CodeOrders.FindAsync(id);
        if (localOrder == null) return null;
        int currentStatus = localOrder.Status;

        if (localOrder.ExternalGuid == null) return null;

        var externalOrder = localOrder.Type == 3
            ? await _externalEmission.GetContainerEmissionInfo(localOrder.ExternalGuid.Value)
            : await _externalEmission.GetEmissionInfo(localOrder.ExternalGuid.Value);

        if (!externalOrder.Success || externalOrder.Content == null) return null;

        localOrder.Status = externalOrder.Content.status switch
        {
            0 => -2,
            1 => 2,
            3 => 3,
            4 => 4,
            5 => -3,
            _ => -4
        };

        if (currentStatus != localOrder.Status)
            localOrder.StatusHistoryJson = [.. localOrder.StatusHistoryJson, new StatusHistory { Status = localOrder.Status, StatusDate = DateTimeOffset.UtcNow }];

        _context.Entry(localOrder).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return localOrder;
    }

    public async Task<CodeOrder?> ProcessCodeEmissionAsync(int id)
    {
        var localOrder = await _context.CodeOrders.FindAsync(id);
        if (localOrder == null) return null;
        if ((localOrder.Status != 2) && (localOrder.Status != 1)) return null;
        if (localOrder.ExternalGuid == null) return null;

        var response = localOrder.Type == 3
            ? await _externalEmission.ProcessContainerEmission(new ProcessDocument { uuids = [localOrder.ExternalGuid.Value] })
            : await _externalEmission.ProcessCodeEmission(new ProcessDocument { uuids = [localOrder.ExternalGuid.Value] });

        if (!response.Success) return null;

        localOrder.Status = 3;
        localOrder.StatusHistoryJson = [.. localOrder.StatusHistoryJson, new StatusHistory { Status = 3, StatusDate = DateTimeOffset.UtcNow }];

        _context.Entry(localOrder).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return localOrder;
    }

    public async Task<CodeOrder?> GetCodesFromOrderAsync(int id)
    {
        var localOrder = await _context.CodeOrders.FindAsync(id);
        if (localOrder == null) return null;
        var product = await _context.Products.FindAsync(localOrder.ProductId);
        if ((localOrder.Status != 4) && (localOrder.Status != 5)) return null;
        if (localOrder.ExternalGuid == null) return null;

        var response = localOrder.Type == 3
            ? await _externalEmission.GetCodesFromContainerEmission(new DownloadCodesRequest { type = 0, uuid = localOrder.ExternalGuid.Value })
            : await _externalEmission.GetCodesFromEmission(new DownloadCodesRequest { type = product.Type, uuid = localOrder.ExternalGuid.Value });

        if (!response.Success || response.Content?.codes == null) return null;

        _context.CodeOrdersContents.Add(new CodeOrderContent
        {
            Id = localOrder.Id,
            CodeOrderId = localOrder.Id,
            OrderContent = response.Content.codes,
            RecordDate = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
        return localOrder;
    }

    public async Task<CodeOrder?> CreateOrderAsync(OrderCreateForm order)
    {
        if (order == null) return null;
        if (order.MarkingLineUuid == null)
        {
            var markingLine = await _context.MarkingLines.FirstOrDefaultAsync();
            if (markingLine == null)
            {
                _logger.LogError("No marking line found in the database.");
                return null;
            }
            order.MarkingLineUuid = markingLine?.ExternalUid;
        }
        if (order.FactoryUuid == null)
        {
            var factory = await _context.Factories.FirstOrDefaultAsync();
            if (factory == null)
            {
                _logger.LogError("No factory found in the database.");
                return null;
            }
            order.FactoryUuid = factory?.ExternalUid;
        }

        var localOrder = new CodeOrder
        {
            Count = order.CodesCount,
            Description = order.Description,
            ProductId = order.ProductId,
            User = order.User,
            Status = 0,
            StatusHistoryJson = new[] { new StatusHistory { Status = 0, StatusDate = DateTimeOffset.UtcNow } },
            Type = order.Type,
            BatchId = order.BatchId
        };

        _context.CodeOrders.Add(localOrder);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving order to the database.");
            return null;
        }

        await Task.Delay(500);

        var emissionRequest = new EmissionCreateRequest
        {
            codesCount = order.CodesCount,
            productUuid = order.ProductUuid,
            markingLineUuid = order.MarkingLineUuid,
            factoryUuid = order.FactoryUuid,
            Type = (order.Type == 3 ? (sbyte)0 : order.Type),
            format = order.Type switch
            {
                0 => _configuration.GetValue<int?>("TJConnection:EmissionCodeFormat:Pack"),
                1 => _configuration.GetValue<int?>("TJConnection:EmissionCodeFormat:Bundle"),
                _ => null
            }
        };

        var result = order.Type == 3
            ? await _externalEmission.CreateContainerEmission(emissionRequest)
            : await _externalEmission.CreateCodeEmission(emissionRequest);

        if (!result.Success || result.Content?.uuid == null)
        {
            localOrder.Status = -1;
            localOrder.StatusMessage = result.Message ?? "Blank result from state system";
            localOrder.StatusHistoryJson = localOrder.StatusHistoryJson
                .Append(new StatusHistory { Status = -1, StatusDate = DateTimeOffset.UtcNow }).ToArray();

            _context.Entry(localOrder).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return null;
        }

        localOrder.Status = 1;
        localOrder.ExternalGuid = result.Content.uuid;
        localOrder.StatusHistoryJson = [.. localOrder.StatusHistoryJson, new StatusHistory { Status = 1, StatusDate = DateTimeOffset.UtcNow }];

        _context.Entry(localOrder).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return localOrder;
    }

    public async Task<CodeOrderContent?> DownloadOrderContentAsync(int id, string user)
    {
        var orderContent = await _context.CodeOrdersContents.FirstOrDefaultAsync(x => x.CodeOrderId == id);
        if (orderContent?.OrderContent == null) return null;

        orderContent.DownloadHistory = new DownloadHistory
        {
            DownloadTime = DateTimeOffset.UtcNow,
            User = user
        };

        await _context.SaveChangesAsync();
        return orderContent;
    }
}