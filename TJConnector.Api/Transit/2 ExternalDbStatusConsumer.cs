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

        var dbInfoList = await _externalDBData.GetContainerInfo(new List<string> { package.Code });

        if (dbInfoList.Content is null)
        {
            package.Comment = "SQL query error";
            package.AddStatus(-2);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        var dbInfo = dbInfoList.Content.Content.FirstOrDefault();

        if (dbInfo == null)
        {
            package.Comment = "Package doesn't exist in external database";
            package.AddStatus(-2);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        if (dbInfo.ExternalDbStatus != 1)
        {
            package.Comment = dbInfo.ExternalDbStatusMessage;
            package.AddStatus(-2);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        package.Status = 2;
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await container.Publish(new ExternalDbContentBody3 { Container = package });
    }
}
