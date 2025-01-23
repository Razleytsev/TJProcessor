using Microsoft.AspNetCore.Mvc;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;
using TJConnector.Web.Services.Contracts;

namespace TJConnector.Web.Services.Implementation;

public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(HttpClient httpClient, ILogger<OrderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<CodeOrder>> GetOrdersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/order");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<CodeOrder>>() ?? Array.Empty<CodeOrder>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch orders.");
            throw;
        }
    }

    public async Task<CodeOrder> GetOrderByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/order/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CodeOrder>() ?? throw new InvalidOperationException("Order not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to fetch order with ID {id}.");
            throw;
        }
    }

    public async Task<CustomResult<DocumentCreateResponse>> CreateOrderAsync(OrderCreateForm form)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/order", form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomResult<DocumentCreateResponse>>() ?? throw new InvalidOperationException("Failed to create order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order.");
            throw;
        }
    }

    public async Task<CustomResult<ProcessResponse>> ProcessOrderAsync(int uuid)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/process", new { });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomResult<ProcessResponse>>() ?? throw new InvalidOperationException("Failed to process order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process order with UUID {uuid}.");
            throw;
        }
    }

    public async Task<CustomResult<EmissionCodesResponse>> DownloadCodesAsync(int uuid)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/download", new { });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomResult<EmissionCodesResponse>>() ?? throw new InvalidOperationException("Failed to download codes.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to download codes for UUID {uuid}.");
            throw;
        }
    }

    public async Task<IActionResult> DownloadOrderContentAsync(int id, string user)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/order/{id}/download?user={user}", new { });
            response.EnsureSuccessStatusCode();
            return new FileContentResult(await response.Content.ReadAsByteArrayAsync(), "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to download content for order {id}.");
            throw;
        }
    }
}