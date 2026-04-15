using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TJConnector.Api.Hubs;
using TJConnector.Api.TransitBatches;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.SharedLibrary.Models;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Controllers;


[ApiController]
[Route("api/batch")]
public class BatchController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderController> _logger;
    private readonly IBusControl _bus;

    public BatchController(
        ApplicationDbContext context,
        ILogger<OrderController> logger,
        IBusControl bus)
    {
        _context = context;
        _logger = logger;
        _bus = bus;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Batch>>> GetBatches()
    {
        return await _context.Batches.Include(b => b.Product).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BatchDTO>> GetBatchById(int id)
    {
        var batch = await _context.Batches
            .Include(b => b.CodeOrders)
            .Include(b => b.Product)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch == null)
        {
            _logger.LogWarning($"Batch with ID {id} not found.");
            return NotFound();
        }

        var dto = await BatchDTOExtensions.ToBatchDTOAsync(batch, _context);
        return Ok(dto);
    }
    [HttpPost("{id}/download")]
    public async Task<IActionResult> DownloadBatchContent(int id, [FromQuery] string user)
    {
        var batch = await _context.Batches
            .Include(b => b.CodeOrders)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (batch == null || batch.CodeOrders == null)
        {
            _logger.LogWarning($"Batch with ID {id} not found.");
            return NotFound();
        }

        if (batch.Status != 2)
        {
            _logger.LogWarning($"Batch is not ready, can't download content");
            return BadRequest();
        }

        string content = "";
        foreach (CodeOrder order in batch.CodeOrders)
        {
            var orderContent = await _context.CodeOrdersContents.FirstOrDefaultAsync(x => x.Id == order.Id);

            if (orderContent?.OrderContent == null)
            {
                _logger.LogWarning($"Order content for ID {id} not found.");
                continue;
            }

            orderContent.DownloadHistory = new DownloadHistory
            {
                DownloadTime = DateTimeOffset.UtcNow,
                User = user
            };

            if (!string.IsNullOrEmpty(content))
            {
                content += Environment.NewLine;
            }
            content += string.Join(Environment.NewLine, orderContent.OrderContent.Select(GS1CodeHelper.StripGroupSeparators));
        }

        await _context.SaveChangesAsync();

        return File(Encoding.UTF8.GetBytes(content), "text/plain", $"codes_{id}.txt");
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessBatch(int id)
    {
        var batch = await _context.Batches
            .FirstOrDefaultAsync(b => b.Id == id);
        if (batch == null)
        {
            _logger.LogWarning($"Batch with ID {id} not found.");
            return NotFound();
        }
        await _bus.Publish(new ProcessBatch { BatchId = batch.Id });
        return Ok(batch);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelBatch(int id)
    {
        var batch = await _context.Batches
            .FirstOrDefaultAsync(b => b.Id == id);
        if (batch == null)
        {
            _logger.LogWarning($"Batch with ID {id} not found.");
            return NotFound();
        }
        batch.Status = -1;
        batch.StatusHistoryJson = batch.StatusHistoryJson.Append(new StatusHistory
        {
            Status = -1,
            StatusDate = DateTimeOffset.UtcNow
        }).ToArray();

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Batch {id} status changed to cancelled.");
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating batch status to cancelled.");
            return BadRequest(ex.Message);
        }
        return Ok(batch);
    }

    [HttpPost]
    public async Task<ActionResult<Batch>> CreateBatch([FromBody] BatchCreateForm formData)
    {
        if (formData == null)
        {
            _logger.LogError("Batch create request body cannot be null.");
            return BadRequest("Request body is required.");
        }

        var batch = new Batch
        {
            Count = formData.CodesCount,
            Description = formData.Description,
            ProductId = formData.ProductId,
            User = formData.User,
            Status = 0,
            StatusHistoryJson = new[] { new StatusHistory { Status = 0, StatusDate = DateTimeOffset.UtcNow } },
            Type = formData.Type,
        };

        _context.Batches.Add(batch);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving batch to the database.");
            return BadRequest(ex.Message);
        }

        await _bus.Publish(new ProcessBatch { BatchId = batch.Id });

        return Ok(batch);
    }
}