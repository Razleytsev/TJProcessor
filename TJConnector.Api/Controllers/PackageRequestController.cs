using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TJConnector.Postgres.Entities;
using TJConnector.Postgres;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;
using MassTransit;
using TJConnector.Api.Hubs;

namespace TJConnector.Api.Controllers;

[ApiController]
[Route("api/packagerequest")]
public class PackageRequestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalContainer _externalContainer;
    private readonly IExternalEmission _externalEmission;
    private readonly ILogger<PackageRequestController> _logger;
    private readonly IBusControl _bus;


    public PackageRequestController(
        ApplicationDbContext context,
        IExternalContainer externalContainer,
        IExternalEmission externalEmission,
        ILogger<PackageRequestController> logger,
        IBusControl bus)

    {
        _context = context;
        _externalEmission = externalEmission;
        _externalContainer = externalContainer;
        _logger = logger;
        _bus = bus;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PackageRequest>>> GetOrders()
    {
        return await _context.PackageRequests.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<PackageRequest>> CreateOrder([FromBody] PackageRequestForm request)
    {
        if (request == null)
        {
            _logger.LogError("Order create request body cannot be null.");
            return BadRequest("Request body is required.");
        }

        var localRequest = new PackageRequest
        {
            Filename = request.Filename,
            User = request.User,
            Status = 0
        };

        _context.PackageRequests.Add(localRequest);

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


        var localPackages = new List<Package>();

        foreach(PackageCouple link in request.packages)
        {
            localPackages.Add(new Package
            {
                Code = link.Code,
                SSCCCode = link.SSCCCode,
                Status = 0,
                PackageRequestId = localRequest.Id
            });
        }

        _context.Packages.AddRange(localPackages);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving order to the database.");
            return BadRequest(ex.Message);
        }

        await _bus.Publish(new OrderCreated { OrderId = localRequest.Id, ContainerIds = localPackages.Select(p => p.Id).ToList() });

        return Ok(localRequest);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PackageRequest>> GetRequestById(int id)
    {
        var request = await _context.PackageRequests.FindAsync(id);

        if (request == null)
        {
            _logger.LogWarning($"Package request with ID {id} not found.");
            return NotFound();
        }

        var requestPackages = await _context.Packages.Where(x => x.PackageRequestId == id).ToListAsync();

        request.Packages = requestPackages;

        return request;
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

        if (localOrder.Type == 3)
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
}
