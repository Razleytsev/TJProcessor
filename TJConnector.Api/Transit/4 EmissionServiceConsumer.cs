using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class StateCreateApplication : IConsumer<StateCreateApplicationBody4>
{
    private readonly IExternalEmission _emissionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StateCreateApplication> _logger;

    public StateCreateApplication(IExternalEmission emissionService, ApplicationDbContext context, ILogger<StateCreateApplication> logger)
    {
        _emissionService = emissionService;
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StateCreateApplicationBody4> container)
    {
        var package = container.Message.Container;

        _logger.LogInformation($"Sending application: {package.SSCCCode}");
        package.Status = -4;

        var factory = await _context.Factories.FindAsync(1);
        var markingLine = await _context.MarkingLines.FindAsync(1);
        var location = await _context.Locations.FirstOrDefaultAsync();

        if(factory == null ||
            markingLine == null ||
            location == null)
        {
            package.Comment = "Check metadata (factory, marking line, location)";
            package.AddStatus(-4);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        if (package.Content == null)
        {
            package.Comment = "No content in database for this package";
            package.AddStatus(-4);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        var groupCodes = new List<GroupCode>(package.Content.Count);
        var badSamples = new List<string>();
        foreach (var entry in package.Content)
        {
            if (!GS1CodeHelper.TryInsertGroupSeparator(entry.Bundle, out var bundleWithGs))
            {
                if (badSamples.Count < 5) badSamples.Add(entry.Bundle);
                continue;
            }
            groupCodes.Add(new GroupCode
            {
                groupCode = bundleWithGs,
                codes = entry.Packs.ToArray()
            });
        }

        if (badSamples.Count > 0)
        {
            var sample = string.Join(", ", badSamples);
            _logger.LogError("Malformed bundle codes for package {Sscc}: {Samples}", package.SSCCCode, sample);
            package.Comment = $"Malformed bundle codes (samples): {sample}";
            package.AddStatus(-4);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        var applicationBody = new ApplicationCreateRequest
        {
            applicationDate = DateTimeOffset.UtcNow.AddHours(-4),
            productionDate = DateTimeOffset.UtcNow,
            factoryUuid = factory.ExternalUid,
            markingLineUuid = markingLine.ExternalUid,
            locationUuid = location.ExternalUid,
            result = 0,
            type = 2,
            groupCodes = groupCodes
        };

        var response = await _emissionService.CreateCodeApplication(applicationBody);

        if (!response.Success)
        {
            package.Comment = response.Message ?? "No error text provided";
            package.AddStatus(-4);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        package.Status = 4;
        package.AddStatus(4);
        package.ContentApplicationGuid = response.Content?.uuid;
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await Task.Delay(1000);

        await container.Publish(new StateApplicationStatusBody5 { Container = package });
    }
}