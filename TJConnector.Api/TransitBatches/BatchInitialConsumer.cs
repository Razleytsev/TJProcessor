using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.TransitBatches;

public class BatchInitialConsumer : IConsumer<ProcessBatch>
{
    private readonly IExternalEmission _emissionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BatchInitialConsumer> _logger;

    public BatchInitialConsumer(IExternalEmission emissionService, ApplicationDbContext externalDb, ILogger<BatchInitialConsumer> logger)
    {
        _emissionService = emissionService;
        _context = externalDb;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessBatch> container)
    {
        var batch = await _context.Batches
            .FirstOrDefaultAsync(b => b.Id == container.Message.BatchId);
        if (batch == null)
        {
            _logger.LogWarning($"Batch with id {container.Message.BatchId} not found");
            return;
        }

        batch.CodeOrders = await _context.CodeOrders
            .Where(o => o.BatchId == batch.Id)
            .ToListAsync();

        if (batch.Status == 0)
        {
            if (batch.CodeOrders == null || batch.CodeOrders.Count == 0)
                await container.Publish(new CreateOrdersForBatch { Batch = batch });
            else
                _logger.LogWarning($"Batch {container.Message.BatchId} already has orders, skipping order creation");
            return;
        }

        if (batch.Status == 1)
        {
            await Task.Delay(5000);
            await container.Publish(new ProcessOrdersForBatch { Batch = batch });
            return;
        }
        if (batch.Status == 2)
        {
            _logger.LogWarning($"Batch {container.Message.BatchId} processed");
            return;
        }
        if (batch.Status == -1)
        {
            _logger.LogWarning($"Batch {container.Message.BatchId} status set to canceled");
            return;
        }
    }
}