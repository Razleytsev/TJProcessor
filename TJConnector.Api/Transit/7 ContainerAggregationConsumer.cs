using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.Container;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class StateCreateAggregation : IConsumer<StateCreateAggregationBody7>
{
    private readonly IExternalContainer _containerService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StateCreateAggregation> _logger;
    public StateCreateAggregation(IExternalContainer containerService, ApplicationDbContext externalDb, ILogger<StateCreateAggregation> logger)
    {
        _containerService = containerService;
        _context = externalDb;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StateCreateAggregationBody7> container)
    {
        var package = container.Message.Container;

        _logger.LogInformation($"Sending aggregation: {package.SSCCCode}");
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

        var bundleCodes = new List<string>(package.Content.Count);
        var badSamples = new List<string>();
        foreach (var entry in package.Content)
        {
            if (!GS1CodeHelper.TryInsertGroupSeparator(entry.Bundle, out var bundleWithGs))
            {
                if (badSamples.Count < 5) badSamples.Add(entry.Bundle);
                continue;
            }
            bundleCodes.Add(bundleWithGs);
        }

        if (badSamples.Count > 0)
        {
            var sample = string.Join(", ", badSamples);
            _logger.LogError("Malformed bundle codes for package {Sscc}: {Samples}", package.SSCCCode, sample);
            package.Comment = $"Malformed bundle codes (samples): {sample}";
            package.AddStatus(-8);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        var aggregationBody = new ContainerOperationCreateRequest
        {
            codes = bundleCodes.ToArray(),
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

        await Task.Delay(5000);

        await container.Publish(new StateAggregationStatusBody8 { Container = package });

    }
}