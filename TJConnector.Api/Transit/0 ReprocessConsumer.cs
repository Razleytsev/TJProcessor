using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class ReprocessConsumer : IConsumer<ReprocessContainer0>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReprocessConsumer> _logger;

    public ReprocessConsumer(ApplicationDbContext context, ILogger<ReprocessConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReprocessContainer0> container)
    {
        var package = container.Message.Container;
        if (package == null) return;

        var dbPackage = await _context.Packages.FindAsync(package.Id);
        if (dbPackage == null) return;

        _logger.LogInformation("Reprocessing package {Id} (status {Status})", dbPackage.Id, dbPackage.Status);

        switch (dbPackage.Status)
        {
            case 0:
            case -1:
                await container.Publish(new StateCheckSSCCBody1 { Containers = new List<Package> { dbPackage } });
                break;
            case -2:
                await container.Publish(new ExternalDbBody2 { Container = dbPackage });
                break;
            case -3:
                await container.Publish(new ExternalDbContentBody3 { Container = dbPackage });
                break;
            case -4:
                await container.Publish(new StateCreateApplicationBody4 { Container = dbPackage });
                break;
            case -5:
            case -6:
                await container.Publish(new StateApplicationStatusBody5 { Container = dbPackage });
                break;
            case -7:
                await container.Publish(new StateApplicationProcessBody6 { Container = dbPackage });
                break;
            case -8:
                await container.Publish(new StateCreateAggregationBody7 { Container = dbPackage });
                break;
            case -9:
                await container.Publish(new StateProcessAggregationBody9 { Container = dbPackage });
                break;
            case -10:
            case -11:
            case -12:
                await container.Publish(new StateAggregationStatusBody8 { Container = dbPackage });
                break;
            default:
                _logger.LogWarning("Package {Id} has status {Status}, cannot reprocess", dbPackage.Id, dbPackage.Status);
                break;
        }
    }
}