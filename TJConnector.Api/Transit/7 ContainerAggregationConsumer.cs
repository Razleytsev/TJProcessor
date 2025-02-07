using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Model.ExternalRequests.Container;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class ContainerAggregationConsumer : IConsumer<ProcessContainerAggregation7>
{
    private readonly IExternalContainer _containerService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContainerAggregationConsumer> _logger;
    public ContainerAggregationConsumer(IExternalContainer containerService, ApplicationDbContext externalDb, ILogger<ContainerAggregationConsumer> logger)
    {
        _containerService = containerService;
        _context = externalDb;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessContainerAggregation7> container)
    {
        var package = container.Message.Container;

        _logger.LogWarning($"ContainerAggregationConsumer{package.SSCCCode}");
        package.Status = -8;

        var location = await _context.Locations.FindAsync(1);

        if (location == null)
        {
            package.Comment = "Check metadata (location)";
            package.AddStatus(-8);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        if (package.Content == null)
        {
            package.Comment = "No content in database for this package";
            package.AddStatus(-8);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        var aggregationBody = new ContainerOperationCreateRequest
        {
            codes = package.Content.Select(x => x.Bundle).ToArray(),
            containerCode = package.SSCCCode,
            locationUuid = location.ExternalUid,
            transferCodes = new string[0],
            type = 0
        };

        var response = await _containerService.ContainerOperation(aggregationBody);

        if (!response.Success || response.Content == null)
        {
            package.Comment = response.Message ?? "Error message failed";
            package.AddStatus(-8);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        package.AggregationGuid = response.Content.uuid;
        package.AddStatus(8);
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await Task.Delay(1000);

        await container.Publish(new ProcessAggregationDocumentStatus8 { Container = package });

    }
}