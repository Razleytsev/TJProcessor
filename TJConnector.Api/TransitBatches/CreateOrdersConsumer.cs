using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Services;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;

namespace TJConnector.Api.TransitBatches;

public class CreateOrdersConsumer : IConsumer<CreateOrdersForBatch>
{
    private readonly ILogger<CreateOrdersConsumer> _logger;
    private readonly IOrderService _orderService;
    private readonly ApplicationDbContext _context;

    public CreateOrdersConsumer(
        ILogger<CreateOrdersConsumer> logger,
        IOrderService orderService,
        ApplicationDbContext context)
    {
        _logger = logger;
        _orderService = orderService;
        _context = context;
    }

    public async Task Consume(ConsumeContext<CreateOrdersForBatch> container)
    {
        var batch = container.Message.Batch;
        int totalCount = batch.Count;
        int maxPerOrder = 10000;
        int createdOrders = 0;

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == batch.ProductId);
        if (product == null && batch.Type != 3)
        {
            _logger.LogError($"Product with ID {batch.ProductId} not found for batch {batch.Id}.");
            return;
        }

        int orderIndex = 0;
        while (totalCount > 0)
        {
            int codesCount = Math.Min(maxPerOrder, totalCount);

            var orderForm = new OrderCreateForm
            {
                CodesCount = codesCount,
                ProductUuid = product?.ExternalUid,
                MarkingLineUuid = null, 
                FactoryUuid = null,  
                Type = (sbyte)batch.Type,
                Description = batch.Description ?? $"Batch {batch.Id}",
                ProductId = batch.ProductId,
                User = batch.User ?? "system",
                BatchId = batch.Id
            };

            try
            {
                var result = await _orderService.CreateOrderAsync(orderForm);
                if (result == null)
                {
                    _logger.LogError($"Failed to create order for batch {batch.Id}, part {orderIndex + 1}.");
                }
                else
                {
                    createdOrders++;
                    _logger.LogInformation($"Batch {batch.Id}: created order {orderIndex + 1} ({codesCount} codes).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while creating order for batch {batch.Id}, part {orderIndex + 1}.");
            }

            totalCount -= codesCount;
            orderIndex++;

            // Inter-order delay: avoid hammering the external emission API with back-to-back requests
            if (totalCount > 0)
                await Task.Delay(2000);
        }

        batch.Status = 1;

        _context.Entry(batch).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created {createdOrders} orders for batch {batch.Id}.");

        await container.Publish(new ProcessBatch { BatchId = batch.Id });
    }
}