using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
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

        if(factory == null ||  
            markingLine == null)
        {
            package.Comment = "Check metadata (factory, marking line)";
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

        var applicationBody = new ApplicationCreateRequest
        {
            applicationDate = DateTimeOffset.UtcNow.AddHours(-4),
            factoryUuid = factory.ExternalUid,
            markingLineUuid = markingLine.ExternalUid,
            result = 0,
            type = 2,
            groupCodes = package.Content.Select(x => new GroupCode()
            {
                groupCode = x.Bundle,
                codes = x.Packs.ToArray()
            }).ToList()
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