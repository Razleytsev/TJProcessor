using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class ExternalDbCheck : IConsumer<ExternalDbBody2>
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalDBData _externalDBData;
    private readonly ILogger<ExternalDbCheck> _logger;

    public ExternalDbCheck(ApplicationDbContext externalDb,IExternalDBData externalDBData, ILogger<ExternalDbCheck> logger)
    {
        _context = externalDb;
        _externalDBData = externalDBData;
        _logger = logger;   
    }

    public async Task Consume(ConsumeContext<ExternalDbBody2> container)
    {
        var package = container.Message.Container;

        package.Status = -2;
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Checking MC {Code} (SSCC {Sscc}) in external database", package.Code, package.SSCCCode);

        var dbInfoList = await _externalDBData.GetContainerInfo(new List<string> { package.Code });

        if (dbInfoList.Content is null)
        {
            package.Comment = $"SQL query error for MC {package.Code}: {dbInfoList.Message}";
            package.AddStatus(-2);
            _context.Entry(package).State = EntityState.Modified;
            _logger.LogWarning("SQL query error for MC {Code} (SSCC {Sscc}): {Error}", package.Code, package.SSCCCode, dbInfoList.Message);
            await _context.SaveChangesAsync();
            return;
        }

        var dbInfo = dbInfoList.Content.Content.FirstOrDefault();

        if (dbInfo == null)
        {
            package.Comment = $"MC {package.Code} not found in external database";
            package.AddStatus(-2);
            _context.Entry(package).State = EntityState.Modified;
            _logger.LogWarning("MC {Code} not found in external database (SSCC {Sscc})", package.Code, package.SSCCCode);
            await _context.SaveChangesAsync();
            return;
        }

        if (dbInfo.ExternalDbStatus != 1)
        {
            package.Comment = $"MC {package.Code}: {dbInfo.ExternalDbStatusMessage}";
            package.AddStatus(-2);
            _context.Entry(package).State = EntityState.Modified;
            _logger.LogWarning("MC {Code} incorrect status in external database (SSCC {Sscc}): {StatusMsg}", package.Code, package.SSCCCode, dbInfo.ExternalDbStatusMessage);
            await _context.SaveChangesAsync();
            return;
        }

        package.Status = 2;
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await container.Publish(new ExternalDbContentBody3 { Container = package });
    }
}
