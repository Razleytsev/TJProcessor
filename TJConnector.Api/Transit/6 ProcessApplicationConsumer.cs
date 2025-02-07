using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class ProcessApplicationConsumer : IConsumer<ProcessApplicationRequest6>
{
    private readonly IExternalEmission _emissionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProcessApplicationConsumer> _logger;

    public ProcessApplicationConsumer(IExternalEmission emissionService, ApplicationDbContext externalDb, ILogger<ProcessApplicationConsumer> logger)
    {
        _emissionService = emissionService;
        _context = externalDb;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessApplicationRequest6> container)
    {
        var package = container.Message.Container;

        _logger.LogWarning($"ProcessAPPLICATIONCONSUMER{package.SSCCCode}");
        var response = await _emissionService.ProcessCodeApplication
            (new ProcessDocument { uuids = new Guid[] { package.ContentApplicationGuid.Value } });

        //if (response.Content?.ProcessResult == null || package.ContentApplicationGuid == null)
        //{
        //    package.Status = -7;
        //    package.AddStatus(-7);
        //    package.Comment = "Failed to process application request";
        //    _context.Entry(package).State = EntityState.Modified;
        //    await _context.SaveChangesAsync();
        //    return;
        //}
        var errorMessage = response.Content?.ProcessResult?[package.ContentApplicationGuid.Value];

        if (errorMessage?.message != null || !response.Success)
        {
            package.Status = -7;
            package.AddStatus(-7);
            package.Comment = "Failed to process application request";
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        package.Status = -7;
        package.AddStatus(-7);
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await Task.Delay(1000);
        await container.Publish(new ProcessAggregationStatus5 { Container = package });
    }
}