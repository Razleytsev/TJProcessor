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

    public async Task<CustomResult<CodeOrder>> CreateOrderAsync(OrderCreateForm form)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/order", form);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("CreateOrderAsync Response: {Response}", responseContent);
            return await response.Content.ReadFromJsonAsync<CustomResult<CodeOrder>>() ?? throw new InvalidOperationException("Failed to create order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order.");
            throw;
        }
    }

    public async Task<CustomResult<ProcessResponse>> ProcessOrderAsync(int uuid)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/process", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomResult<ProcessResponse>>() ?? throw new InvalidOperationException("Failed to process order.");
    }

    public async Task<CustomResult<EmissionCodesResponse>> DownloadCodesAsync(int uuid)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/download", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomResult<EmissionCodesResponse>>() ?? throw new InvalidOperationException("Failed to download codes.");
    }

    public async Task<IActionResult> DownloadOrderContentAsync(int id, string user)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/order/{id}/download?user={user}", new { });
        response.EnsureSuccessStatusCode();
        return new FileContentResult(await response.Content.ReadAsByteArrayAsync(), "text/plain");
    }
    public async Task<CustomResult<CodeOrder>> GetExternalOrderByIdAsync(int id)
    {
        try
        {
            // Call the API endpoint to fetch external order status
            var response = await _httpClient.GetAsync($"api/order/external/{id}");

            // Ensure the request was successful
            response.EnsureSuccessStatusCode();

            // Deserialize the response content
            var result = await response.Content.ReadFromJsonAsync<CustomResult<CodeOrder>>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize external order response.");
                return new CustomResult<CodeOrder> { Success = false, Message = "Failed to deserialize response." };
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch external order status for ID {Id}.", id);
            return new CustomResult<CodeOrder> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching external order status for ID {Id}.", id);
            return new CustomResult<CodeOrder> { Success = false, Message = "An unexpected error occurred." };
        }
    }
}