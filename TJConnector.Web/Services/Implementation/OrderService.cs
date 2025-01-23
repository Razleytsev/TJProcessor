using Microsoft.AspNetCore.Mvc;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;
using TJConnector.Web.Services.Contracts;

namespace TJConnector.Web.Services.Implementation
{
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
            return await response.Content.ReadFromJsonAsync<CodeOrder>();
        }

        public async Task<CustomResult<DocumentCreateResponse>> CreateOrderAsync(OrderCreateForm form)
        {
            var response = await _httpClient.PostAsJsonAsync("api/order", form);
            return await response.Content.ReadFromJsonAsync<CustomResult<DocumentCreateResponse>>();
        }

        public async Task<CustomResult<ProcessResponse>> ProcessOrderAsync(Guid uuid)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/process", new { });
            return await response.Content.ReadFromJsonAsync<CustomResult<ProcessResponse>>();
        }

        public async Task<CustomResult<EmissionCodesResponse>> DownloadCodesAsync(Guid uuid)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/order/external/{uuid}/download", new { });
            return await response.Content.ReadFromJsonAsync<CustomResult<EmissionCodesResponse>>();
        }

        public async Task<IActionResult> DownloadOrderContentAsync(int id, string user)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/order/{id}/download?user={user}", new { });
            return response.IsSuccessStatusCode ? new FileContentResult(await response.Content.ReadAsByteArrayAsync(), "text/plain") : (IActionResult)BadRequest();
        }
    }
}
