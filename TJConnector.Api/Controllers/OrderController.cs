 using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Controllers;

[ApiController]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _externalEmission;
    private readonly ILogger<OrderController> _logger;

    //private readonly IHubContext<OrderHub> _hubContext;

    public OrderController(
        ApplicationDbContext context,
        IExternalEmission externalEmission,
        ILogger<OrderController> logger)
    {
        _context = context;
        _externalEmission = externalEmission;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CodeOrder>>> GetOrders()
    {
        return await _context.CodeOrders.Include(order => order.Product).ToListAsync();
    }

    [HttpGet("{id}")]                                                                                                                                   
    public async Task<ActionResult<CodeOrder>> GetOrderById(int id)
    {
        var order = await _context.CodeOrders.FindAsync(id);

        if (order == null)
        {
            _logger.LogWarning($"Order with ID {id} not found.");
            return NotFound();
        }

        var orderContent = await _context.CodeOrdersContents.FirstOrDefaultAsync(x => x.CodeOrderId == id);
        var orderProduct = await _context.Products.FirstOrDefaultAsync(x => x.Id == order.ProductId);

        order.Content = orderContent;
        order.Product = orderProduct;

        return order;
    }

    [HttpGet("external/{id}")] 
    public async Task<ActionResult<CodeOrder>> GetExternalOrderById(int id)
    {
        var localOrder = await _context.CodeOrders.FindAsync(id);

        if (localOrder == null)
        {
            _logger.LogWarning($"Order with ID {id} not found.");
            return NotFound();
        }
        int currentStatus = localOrder.Status;

        if (localOrder.ExternalGuid == null)
        {
            _logger.LogWarning($"Order with ID {id} has no external GUID.");
            return BadRequest("Order not sent to external system.");
        }

        var externalOrder = new CustomResult<EmissionInfoResponse>();

        if (localOrder.Type == 3)
            externalOrder = await _externalEmission.GetContainerEmissionInfo(localOrder.ExternalGuid.Value);
        else
            externalOrder = await _externalEmission.GetEmissionInfo(localOrder.ExternalGuid.Value);

        if (!externalOrder.Success || externalOrder.Content == null)
        {
            _logger.LogError($"Failed to fetch external order info for GUID {localOrder.ExternalGuid}.");
            return BadRequest(externalOrder.Message ?? "Failed to fetch external order info.");
        }

        localOrder.Status = externalOrder.Content.status switch
        {
            0 => -2, // saved_error
            1 => 2,  // saved
            3 => 3,  // executing
            4 => 4,  // available
            5 => -3, // failed
            6 => 5,  // done
            _ => -4  // unknown
        };

        if (currentStatus != localOrder.Status)
            localOrder.StatusHistoryJson = [.. localOrder.StatusHistoryJson, new StatusHistory { Status = localOrder.Status, StatusDate = DateTimeOffset.UtcNow }];

        _context.Entry(localOrder).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(localOrder);
    }

    [HttpPost("external/{id}/process")]
    public async Task<ActionResult<CodeOrder>> ProcessCodeEmission(int id)
    {
        var localOrder = await _context.CodeOrders.FindAsync(id);

        if (localOrder == null)
        {
            _logger.LogWarning($"Order with ID {id} not found.");
            return BadRequest("Order not found.");
        }

        if ((localOrder.Status != 2) && (localOrder.Status != 1))
        {
            _logger.LogWarning($"Order with ID {id} has incorrect status for processing.");
            return BadRequest("Incorrect order status.");
        }

        if (localOrder.ExternalGuid == null)
        {
            _logger.LogWarning($"Order with ID {id} has no external GUID.");
            return BadRequest("Incorrect order status.");
        }

        var response = new CustomResult<ProcessResponse>();

        if(localOrder.Type == 3)
            response = await _externalEmission.ProcessContainerEmission(new ProcessDocument { uuids = [localOrder.ExternalGuid.Value] });
        else 
            response = await _externalEmission.ProcessCodeEmission(new ProcessDocument { uuids = [localOrder.ExternalGuid.Value] });

        if (!response.Success)
        {
            _logger.LogError($"Failed to process emission for order {id}. Message: {response.Message}");
            return BadRequest(response.Message);
        }

        localOrder.Status = 3;
        localOrder.StatusHistoryJson = [.. localOrder.StatusHistoryJson, new StatusHistory { Status = 3, StatusDate = DateTimeOffset.UtcNow }];

        _context.Entry(localOrder).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(localOrder);
    }

    [HttpPost("external/{id}/download")]
    public async Task<ActionResult<CodeOrder>> GetCodesFromOrder(int id)
    {
        var localOrder = await _context.CodeOrders.FindAsync(id);
          // ?? throw new ArgumentNullException($"Order ({id}) not found.");

        if (localOrder == null)
        {
            _logger.LogWarning($"Order with ID {id} not found.");
            return BadRequest("Order not found.");
        }
        var product = await _context.Products.FindAsync(localOrder.ProductId);

        if ((localOrder.Status != 4) && (localOrder.Status != 5))
        {
            _logger.LogWarning($"Order with ID {id} has incorrect status for downloading.");
            return BadRequest("Incorrect order status.");
        }

        if (localOrder.ExternalGuid == null)
        {
            _logger.LogWarning($"Order with ID {id} has no external GUID.");
            return BadRequest("Incorrect order status.");
        }

        var response = new CustomResult<EmissionCodesResponse>();

        if (localOrder.Type == 3)
            response = await _externalEmission.GetCodesFromContainerEmission(new DownloadCodesRequest { type = 0, uuid = localOrder.ExternalGuid.Value });
        else
            response = await _externalEmission.GetCodesFromEmission(new DownloadCodesRequest { type = product.Type, uuid = localOrder.ExternalGuid.Value });

        if (!response.Success || response.Content?.codes == null)
        {
            _logger.LogError($"Failed to download codes for order {id}. Message: {response.Message}");
            return BadRequest(response.Message ?? "Failed to download codes.");
        }

        _context.CodeOrdersContents.Add(new CodeOrderContent
        {
            Id = localOrder.Id,
            CodeOrderId = localOrder.Id,
            OrderContent = response.Content.codes,
            RecordDate = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(localOrder);
    }

    [HttpPost("{id}/download")]
    public async Task<IActionResult> DownloadOrderContent(int id, [FromQuery] string user)
    {
        var orderContent = await _context.CodeOrdersContents.FirstOrDefaultAsync(x => x.CodeOrderId == id);

        if (orderContent?.OrderContent == null)
        {
            _logger.LogWarning($"Order content for ID {id} not found.");
            return NotFound();
        }

        orderContent.DownloadHistory = new DownloadHistory
        {
            DownloadTime = DateTimeOffset.UtcNow,
            User = user
        };

        await _context.SaveChangesAsync();

        var content = string.Join(Environment.NewLine, orderContent.OrderContent);
        return File(Encoding.UTF8.GetBytes(content), "text/plain", $"codes_{id}.txt");
    }

    [HttpPost]
    public async Task<ActionResult<CodeOrder>> CreateOrder([FromBody] OrderCreateForm order)
    {
        if (order == null)
        {
            _logger.LogError("Order create request body cannot be null.");
            return BadRequest("Request body is required.");
        }

        var localOrder = new CodeOrder
        {
            Count = order.CodesCount,
            Description = order.Description,
            ProductId = order.ProductId,
            User = order.User,
            Status = 0,
            StatusHistoryJson = new[] { new StatusHistory { Status = 0, StatusDate = DateTimeOffset.UtcNow } },
            Type = order.Type
        };

        _context.CodeOrders.Add(localOrder);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving order to the database.");
            return BadRequest(ex.Message);
        }

        await Task.Delay(500);

        var emissionRequest = new EmissionCreateRequest
        {
            codesCount = order.CodesCount,
            productUuid = order.ProductUuid,
            markingLineUuid = order.MarkingLineUuid,
            factoryUuid = order.FactoryUuid,
            Type = (order.Type == 3 ? (sbyte)0 : order.Type)
        };

        var result = new CustomResult<DocumentCreateResponse>();
        if (order.Type == 3) 
            result = await _externalEmission.CreateContainerEmission(emissionRequest);
        else
            result = await _externalEmission.CreateCodeEmission(emissionRequest);

        if (!result.Success || result.Content?.uuid == null)
        {
            localOrder.Status = -1;
            localOrder.StatusMessage = result.Message ?? "Blank result from state system";
            localOrder.StatusHistoryJson = localOrder.StatusHistoryJson
                .Append(new StatusHistory { Status = -1, StatusDate = DateTimeOffset.UtcNow }).ToArray();

            _context.Entry(localOrder).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return BadRequest(result.Message ?? "Failed to create emission in external system.");
        }

        localOrder.Status = 1;
        localOrder.ExternalGuid = result.Content.uuid;
        localOrder.StatusHistoryJson = [.. localOrder.StatusHistoryJson, new StatusHistory { Status = 1, StatusDate = DateTimeOffset.UtcNow }];

        _context.Entry(localOrder).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(localOrder);
    }
}