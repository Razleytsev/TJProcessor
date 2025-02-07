using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class ReprocessConsumer : IConsumer<ReprocessContainer0>
{
    private readonly IExternalEmission _emissionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReprocessConsumer> _logger;

    public ReprocessConsumer(IExternalEmission emissionService, ApplicationDbContext externalDb, ILogger<ReprocessConsumer> logger)
    {
        _emissionService = emissionService;
        _context = externalDb;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReprocessContainer0> container)
    {
        var package = container.Message.Container;

        _logger.LogWarning($"Reprocessing {package.Code} {package.SSCCCode} with status {package.Status}");

        if(package.Content == null)
        {
            await container.Publish(new ProcessApplicationRequest6 { Container = package });
            return;
        }
        if (package.AggregationGuid != null)
        {
            await container.Publish(new ProcessAggregationDocumentStatus8 { Container = package });
            return;
        }
        if (package.ContentApplicationGuid != null)
        {
            await container.Publish(new ProcessAggregationStatus5 { Container = package });
            return;
        }
    }
}