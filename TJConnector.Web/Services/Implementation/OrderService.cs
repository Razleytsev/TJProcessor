using Microsoft.AspNetCore.Http.HttpResults;
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
        var response = await _httpClient.GetAsync("api/order");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<CodeOrder>>() ?? Array.Empty<CodeOrder>();
    }

    public async Task<CodeOrder> GetOrderByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/order/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CodeOrder>() ?? throw new InvalidOperationException("Order not found.");
    }

    public async Task<CodeOrder> CreateOrderAsync(OrderCreateForm form)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/order", form);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("CreateOrderAsync Response: {Response}", responseContent);
            return await response.Content.ReadFromJsonAsync<CodeOrder>() ?? throw new InvalidOperationException("Failed to create order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order.");
            throw;
        }
    }

    public async Task<ProcessResponse> ProcessOrderAsync(int uuid)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/process", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProcessResponse>() ?? throw new InvalidOperationException("Failed to process order.");
    }

    public async Task<CodeOrder> DownloadCodesAsync(int uuid)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/download", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CodeOrder>() ?? throw new InvalidOperationException("Failed to download codes.");
    }

    public async Task<IActionResult> DownloadOrderContentAsync(int id, string user)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/order/{id}/download?user={user}", new { });
        response.EnsureSuccessStatusCode();
        return new FileContentResult(await response.Content.ReadAsByteArrayAsync(), "text/plain");
    }
    public async Task<CodeOrder> GetExternalOrderByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/order/external/{id}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CodeOrder>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize external order response.");
                throw new InvalidOperationException("Failed to deserialize external order response."); 
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch external order status for ID {Id}.", id);
            throw new InvalidOperationException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching external order status for ID {Id}.", id);
            throw new InvalidOperationException(ex.Message);
        }
    }
}