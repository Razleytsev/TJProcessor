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
        IBusControl bus
        )

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
        return await _context.PackageRequests.Include(r => r.Packages).ToListAsync();
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

        var localPackages = new List<Package>();

        foreach (PackageCouple link in request.packages)
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

        await _bus.Publish(new StateCheckSSCCBody1 { Containers = localPackages });

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

    [HttpGet("reprocess/{id}")]
    public async Task<ActionResult> ReprocessElement(int id)
    {
        var localPackage = await _context.Packages.FirstOrDefaultAsync(x => x.Id == id);

        if (localPackage == null)
            return NotFound();
        await _bus.Publish(new ReprocessContainer0 { Container = localPackage });

        return Ok();
    }

    [HttpPost("{requestId}/reprocess-created")]
    public async Task<ActionResult> ReprocessCreated(int requestId)
    {
        var packages = await _context.Packages
            .Where(p => p.PackageRequestId == requestId && p.Status == 0)
            .ToListAsync();

        if (!packages.Any())
            return Ok(new { count = 0 });

        await _bus.Publish(new StateCheckSSCCBody1 { Containers = packages });
        _logger.LogInformation("Re-queued {Count} packages at status 0 for request {Id}", packages.Count, requestId);

        return Ok(new { count = packages.Count });
    }

    [HttpGet("test/{id}")]
    public async Task<ActionResult<ContainerInfoResponse>> GetExternalOrderById(string id)
    {
        var result = await _externalContainer.ContainerInfo(id);
        return Ok(result);
    }

    [HttpPost("external/code/find")]
    public async Task<ActionResult<ListResponse<ContainerInfoResponse>>> ProcessCodeEmission(string[] ids)
    {
        try
        {
            var result = await _externalContainer.ContainerInfoList(new ListRequestRequest
            {
                filters = new Filter { code = ids }
            });

            if (result.Content?.statusCode != 200)
            {
                _logger.LogWarning($"Failed to fetch external codes: {result.Message}");
                return BadRequest(result.Message);
            }

            if (result.Content?.items == null)
            {
                _logger.LogWarning("No codes returned from external API.");
                return BadRequest("No codes returned from external API.");
            }

            return Ok(result.Content.items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch external codes.");
            return StatusCode(500, "An error occurred while fetching external code information.");
        }
    }
}
