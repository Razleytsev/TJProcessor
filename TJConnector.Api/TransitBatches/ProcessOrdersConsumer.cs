using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Services;
using TJConnector.Postgres;

namespace TJConnector.Api.TransitBatches;
public class ProcessOrdersConsumer : IConsumer<ProcessOrdersForBatch>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProcessOrdersConsumer> _logger;
    private readonly IOrderService _orderService;
    private readonly IPublishEndpoint _publishEndpoint;

    public ProcessOrdersConsumer(
        ApplicationDbContext context,
        ILogger<ProcessOrdersConsumer> logger,
        IOrderService orderService,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _logger = logger;
        _orderService = orderService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<ProcessOrdersForBatch> container)
    {
        var batch = container.Message.Batch;

        var processingOrders = await _context.CodeOrders
            .Where(o => o.BatchId == batch.Id && o.Status >= 0 && o.Status <= 4)
            .ToListAsync();

        if (processingOrders.Count == 0)
        {
            var dbBatch = await _context.Batches.FindAsync(batch.Id);
            if (dbBatch != null)
            {
                dbBatch.Status = 2;
                await _context.SaveChangesAsync();
            }
            _logger.LogInformation($"Batch {batch.Id} finished, no processing orders left.");
            return;
        }

        foreach (var order in processingOrders)
        {
            try
            {
                switch (order.Status)
                {
                    case 2:
                        var processedOrder = await _orderService.ProcessCodeEmissionAsync(order.Id);
                        if (processedOrder != null)
                        {
                            order.Status = 3;
                            _context.Entry(order).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                        break;

                    case 4:
                        var content = await _context.CodeOrdersContents
                            .FirstOrDefaultAsync(c => c.CodeOrderId == order.Id);

                        if (content == null)
                        {
                            var codesOrder = await _orderService.GetCodesFromOrderAsync(order.Id);
                            if (codesOrder != null)
                            {
                                order.Status = 3;
                                _context.Entry(order).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            order.Status = 5;
                            _context.Entry(order).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                        break;

                    case 0:
                    case 1:
                    case 3:
                        await _orderService.GetExternalOrderByIdAsync(order.Id);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing order {order.Id} in batch {batch.Id}");
            }
        }

        await _publishEndpoint.Publish(new ProcessOrdersForBatch { Batch = batch });
    }
}