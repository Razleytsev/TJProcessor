using Microsoft.AspNetCore.Mvc;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Model.ExternalDbResult;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Controllers;

[ApiController]
[Route("api")]
public class ExternalDbController : ControllerBase
{
    private readonly IExternalDBData _externalDBData;
    private readonly ILogger<ExternalDbController> _logger;

    public ExternalDbController(
        IExternalDBData externalDBData,
        ILogger<ExternalDbController> logger)
    {
        _externalDBData = externalDBData;
        _logger = logger;
    }

    [HttpPost("mcinfo")]
    public async Task<ActionResult<DbContainerStatus>> GetMcInfo(List<string> containers)
    {
        var containerListStatuses = await _externalDBData.GetContainerInfo(containers);
        return Ok(containerListStatuses);
    }

    [HttpGet("mccontent/{container}")]
    public async Task<ActionResult<List<PackageContent>>> GetMcContent(string container)
    {
        var containerContent = await _externalDBData.GetContainerContent(container);
        return Ok(containerContent);
    }
}
