using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.ComponentModel;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class StateApplicationStatus : IConsumer<StateApplicationStatusBody5>
{
    private readonly IExternalEmission _emissionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StateApplicationStatus> _logger;

    public StateApplicationStatus(IExternalEmission emissionService, ApplicationDbContext externalDb, ILogger<StateApplicationStatus> logger)
    {
        _emissionService = emissionService;
        _context = externalDb;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StateApplicationStatusBody5> container)
    {
        var package = container.Message.Container;
        var statusList = await _emissionService.GetCodeApplicationInfo(package.ContentApplicationGuid ?? Guid.Empty);

        _logger.LogInformation($"Checking application status: {package.SSCCCode}");
        var status = statusList.Content;

        if (status == null)
        {
            package.Status = -5;
            package.Comment = "Unknown error with ApplicationInfo";
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        switch (status?.status)
        {
            case 0:
                package.Status = -5;
                package.Comment = "Saved with error in TJ state system";
                package.AddStatus(-5);
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return;
            case 1:
                package.Status = 6;
                package.AddStatus(6);
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await container.Publish(new StateApplicationProcessBody6 { Container = package });
                return;
            case 2:
                package.Status = -5;
                package.AddStatus(-5);
                package.Comment = "Archived in TJ state system";
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return;
            case 3:
                // FIX: post-increment (RetryCount++) returns the OLD value, so retry never advanced.
                // Use pre-computed incremented value so each republish carries the correct count.
                int r = container.Message.RetryCount + 1;
                package.Status = 5;
                package.Comment = $"Processing in TJ state system (attempt {r})";
                package.AddStatus(5);
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Package {package.SSCCCode}: application still processing, retry {r}, waiting {(r < 10 ? r * 6 : 60)} s.");
                await Task.Delay(r < 10 ? r * 6000 : 60000);
                await container.Publish(new StateApplicationStatusBody5 { Container = package, RetryCount = r });
                return;
            case 4:
                package.Status = -6;
                package.AddStatus(-6);
                package.Comment = "Failed in TJ state system";
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return;
            case 5:
                package.Status = 8;
                package.AddStatus(8);
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await container.Publish(new StateCreateAggregationBody7 { Container = package });
                return;
        }

        package.Status = -5;
        package.AddStatus(-5);
        package.Comment = "Unknown status in TJ state system";
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}