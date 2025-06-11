using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;

namespace TJConnector.SharedLibrary.Models;
public class BatchDTO
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;
    public string ProductGtin { get; set; } = string.Empty;

    public int RequestedCodes { get; set; } = 0;
    public int DownloadedCodes { get; set; } = 0;

    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? User { get; set; }

    public DateTimeOffset RecordDate { get; set; }
    public List<OrderDTO>? Orders { get; set; } = new List<OrderDTO>();
}

public class OrderDTO
{
    public int Id { get; set; }
    public string? Type { get; set; } = string.Empty;

    public int RequestedCodes { get; set; } = 0;
    public int DownloadedCodes { get; set; } = 0;

    public string? Status { get; set; } = string.Empty;
    public Guid? ExternalGuid { get; set; }
    public string? Description { get; set; }
    public string? User { get; set; }
    public DateTimeOffset RecordDate { get; set; }
    public StatusHistory[] StatusHistoryJson { get; set; } = new StatusHistory[0];
    public string? StatusMessage { get; set; }
}

public static class BatchDTOExtensions
{
    public static async Task<BatchDTO?> ToBatchDTOAsync(Batch batch, ApplicationDbContext context)
    {
        if (batch == null) return null;

        var orderIds = batch.CodeOrders?.Select(o => o.Id).ToList() ?? new List<int>();
        var downloadedCodesDict = await context.CodeOrdersContents
            .Where(c => orderIds.Contains(c.CodeOrderId))
            .GroupBy(c => c.CodeOrderId)
            .Select(g => new { OrderId = g.Key, DownloadedCodes = g.Sum(x => x.OrderContent.Length) })
            .ToDictionaryAsync(x => x.OrderId, x => x.DownloadedCodes);

        var orderDTOs = batch.CodeOrders?.Select(order =>
        {
            var downloadedCodes = downloadedCodesDict.TryGetValue(order.Id, out var count) ? count : 0;
            return new OrderDTO
            {
                Id = order.Id,
                Type = MapBatchType(batch.Type),
                RequestedCodes = order.Count,
                DownloadedCodes = downloadedCodes,
                Status = MapOrderStatus(order.Status),
                ExternalGuid = order.ExternalGuid,
                Description = order.Description,
                User = order.User,
                RecordDate = order.RecordDate,
                StatusMessage = order.StatusMessage
            };
        }).ToList() ?? new List<OrderDTO>();

        return new BatchDTO
        {
            Id = batch.Id,
            Type = MapBatchType(batch.Type),
            ProductGtin = batch.Product?.Gtin ?? string.Empty,
            RequestedCodes = batch.Count,
            DownloadedCodes = orderDTOs.Sum(o => o.DownloadedCodes),
            Status = MapBatchStatus(batch.Status),
            Description = batch.Description,
            User = batch.User,
            RecordDate = batch.RecordDate,
            Orders = orderDTOs
        };
    }

    private static string MapBatchStatus(int status) => status switch
    {
        0 => "Created",
        1 => "Processing",
        2 => "Completed",
        -1 => "Canceled",
        _ => "Unknown"
    };

    private static string MapBatchType(int type) => type switch
    {
        0 => "Pack",
        1 => "Bundle",
        2 => "Mastercase",
        _ => "Unknown"
    };

    private static string MapOrderStatus(int status) => status switch
    {
        -3 => "External Failed",
        -2 => "External Saved_error",
        -1 => "Sending error",
        0 => "Created",
        1 => "Sent",
        2 => "Received",
        3 => "Executing",
        4 => "Ready",
        5 => "Done",
        _ => "Unknown"
    };

    private static string MapOrderType(int type) => type switch
    {
        0 => "Pack",
        1 => "Bundle",
        2 => "Mastercase",
        _ => "Unknown"
    };
}