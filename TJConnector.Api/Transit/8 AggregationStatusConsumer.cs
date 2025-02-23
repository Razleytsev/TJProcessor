using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;
public class StateAggregationStatus : IConsumer<StateAggregationStatusBody8>

{
    private readonly IExternalContainer _containerService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StateAggregationStatus> _logger;   
    public StateAggregationStatus(IExternalContainer containerService, ApplicationDbContext externalDb, ILogger<StateAggregationStatus> logger)
    {
        _containerService = containerService;
        _context = externalDb;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StateAggregationStatusBody8> container)
    {
        var package = container.Message.Container;
        var statusList = await _containerService.ContainerOperationCheck(package.AggregationGuid.Value);

        _logger.LogWarning($"AggregationStatusConsumer{package.SSCCCode}");
        var status = statusList.Content;

        if (status == null)
        {
            package.Status = -9;
            package.Comment = "Unknown error with AggregationInfo";
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        switch (status?.status)
        {
            case 0:
                package.Status = -10;
                package.Comment = "Saved with error in TJ state system";
                package.AddStatus(-10);
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return;
            case 1:
                package.Status = 10;
                package.AddStatus(10);
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await container.Publish(new StateProcessAggregationBody9 { Container = package });
                return;
            case 2:
                package.Status = -10;
                package.AddStatus(-10);
                package.Comment = "Archived in TJ state system";
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return;
            case 3:
                int r = container.Message.RetryCount++;
                package.Status = 11;
                package.Comment = "Processing in TJ state system";
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await Task.Delay(r < 10 ? r * 6000 : 60000);
                await container.Publish(new StateAggregationStatusBody8 { Container = package, RetryCount = r });
                return;
            case 4:
                package.Status = -11;
                package.AddStatus(-11);
                package.Comment = "Failed in TJ state system";
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return;
            case 5:
                package.Status = 12;
                package.AddStatus(12);
                package.Comment = "Reported";
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return;
        }

        package.Status = -12;
        package.AddStatus(-12);
        package.Comment = "Unknown status in TJ state system";
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}