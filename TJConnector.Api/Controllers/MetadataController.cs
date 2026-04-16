using Microsoft.AspNetCore.Mvc;
using TJConnector.Postgres.Entities;
using TJConnector.Postgres;
using TJConnector.StateSystem.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace TJConnector.Api.Controllers;
[ApiController]
[Route("api")]
public class MetadataController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _externalEmission;
    private readonly ILogger<OrderController> _logger;

    public MetadataController(
        ApplicationDbContext context,
        IExternalEmission externalEmission,
        ILogger<OrderController> logger)
    {
        _context = context;
        _externalEmission = externalEmission;
        _logger = logger;
    }

    [HttpGet("factories")]
    public async Task<ActionResult<IEnumerable<Factory>>> GetFactories()
    {
        return await _context.Factories.ToListAsync();
    }
    [HttpGet("markingline")]
    public async Task<ActionResult<IEnumerable<MarkingLine>>> GetMarkingLine()
    {
        return await _context.MarkingLines.ToListAsync();
    }
    [HttpGet("location")]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocations()
    {
        return await _context.Locations.ToListAsync();
    }
}