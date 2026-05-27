using Microsoft.AspNetCore.Mvc;

namespace TJConnector.Api.Controllers;

/// <summary>
/// Read-only snapshot of the API host's effective configuration so the Web's
/// /settings admin page can render it. The endpoint deliberately exposes raw
/// values (including TJConnection:Token and ConnectionStrings) — it is gated
/// upstream by the Web's [Authorize(Roles = "Admin")] attribute. The API is
/// reachable only from the docker-compose network in production setups; if
/// you publish the API directly, add API-level auth before exposing this.
/// </summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SettingsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public ActionResult<SettingsDto> Get()
    {
        string? v(string key) => _configuration[key];
        string? cs(string key) => _configuration.GetConnectionString(key);

        var dto = new SettingsDto
        {
            Sections = new List<SettingsSection>
            {
                new()
                {
                    Title = "Marking-Authority Connection",
                    Rows = new()
                    {
                        new() { Key = "TJConnection:BaseURL", Value = v("TJConnection:BaseURL") ?? "", IsSensitive = false },
                        new() { Key = "TJConnection:Token",   Value = v("TJConnection:Token") ?? "",   IsSensitive = true  },
                    }
                },
                new()
                {
                    Title = "Emission Code Format",
                    Rows = new()
                    {
                        new() { Key = "TJConnection:EmissionCodeFormat:Pack",   Value = v("TJConnection:EmissionCodeFormat:Pack")   ?? "", IsSensitive = false },
                        new() { Key = "TJConnection:EmissionCodeFormat:Bundle", Value = v("TJConnection:EmissionCodeFormat:Bundle") ?? "", IsSensitive = false },
                    }
                },
                new()
                {
                    Title = "Database",
                    Rows = new()
                    {
                        new() { Key = "ConnectionStrings:LocalDb",    Value = cs("LocalDb")    ?? "", IsSensitive = true },
                        new() { Key = "ConnectionStrings:ExternalDb", Value = cs("ExternalDb") ?? "", IsSensitive = true },
                    }
                },
                new()
                {
                    Title = "Test Run",
                    Rows = new()
                    {
                        new() { Key = "TestRun:Enabled", Value = v("TestRun:Enabled") ?? "false", IsSensitive = false },
                    }
                },
                new()
                {
                    Title = "Logging",
                    Rows = new()
                    {
                        new() { Key = "Logging:LogLevel:Default",                    Value = v("Logging:LogLevel:Default")                    ?? "", IsSensitive = false },
                        new() { Key = "Logging:LogLevel:Microsoft.AspNetCore",       Value = v("Logging:LogLevel:Microsoft.AspNetCore")       ?? "", IsSensitive = false },
                        new() { Key = "Logging:LogLevel:Microsoft.EntityFrameworkCore", Value = v("Logging:LogLevel:Microsoft.EntityFrameworkCore") ?? "", IsSensitive = false },
                    }
                }
            }
        };

        return Ok(dto);
    }
}

public class SettingsDto
{
    public List<SettingsSection> Sections { get; set; } = new();
}

public class SettingsSection
{
    public string Title { get; set; } = string.Empty;
    public List<SettingsRow> Rows { get; set; } = new();
}

public class SettingsRow
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
}
