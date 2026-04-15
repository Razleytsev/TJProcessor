using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Services;
using TJConnector.Postgres;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.SharedLibrary.Models;
using TestRunEntity = TJConnector.Postgres.Entities.TestRun;

namespace TJConnector.Api.Controllers;

[ApiController]
[Route("api/testrun")]
public class TestRunController : ControllerBase
{
    private readonly ITestRunService _service;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestRunController> _logger;

    public TestRunController(
        ITestRunService service,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<TestRunController> logger)
    {
        _service = service;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    private bool IsEnabled() => _configuration.GetValue<bool>("TestRun:Enabled");

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TestRunDto>>> List()
    {
        if (!IsEnabled()) return NotFound();
        var runs = await _service.ListAsync();
        var result = new List<TestRunDto>(runs.Count);
        foreach (var r in runs) result.Add(await ToDto(r));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TestRunDto>> GetById(int id)
    {
        if (!IsEnabled()) return NotFound();
        var run = await _service.GetByIdAsync(id);
        if (run == null) return NotFound();
        return Ok(await ToDto(run));
    }

    [HttpPost]
    public async Task<ActionResult<TestRunDto>> Create([FromBody] TestRunCreateForm form)
    {
        if (!IsEnabled()) return NotFound();
        if (form == null) return BadRequest("Form required");
        var run = await _service.CreateAsync(form);
        if (run == null) return BadRequest("Invalid input: check product IDs and counts");
        return Ok(await ToDto(run));
    }

    [HttpPost("{id}/reprocess/{fromStage}")]
    public async Task<ActionResult<TestRunDto>> Reprocess(int id, int fromStage)
    {
        if (!IsEnabled()) return NotFound();
        var clone = await _service.ReprocessAsync(id, fromStage);
        if (clone == null) return BadRequest("Invalid parent or stage");
        return Ok(await ToDto(clone));
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<TestRunDto>> Cancel(int id)
    {
        if (!IsEnabled()) return NotFound();
        var run = await _service.CancelAsync(id);
        if (run == null) return NotFound();
        return Ok(await ToDto(run));
    }

    private async Task<TestRunDto> ToDto(TestRunEntity run)
    {
        var packGtin = (await _context.Products.FindAsync(run.PackProductId))?.Gtin ?? string.Empty;
        var bundleGtin = (await _context.Products.FindAsync(run.BundleProductId))?.Gtin ?? string.Empty;
        return TestRunDto.From(run, packGtin, bundleGtin);
    }
}
