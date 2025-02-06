using System.Net.Http;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.Web.Services.Contracts;

namespace TJConnector.Web.Services.Implementation
{
    public class PackageRequestService : IPackageRequestService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PackageRequestService> _logger;

        public PackageRequestService(HttpClient httpClient, ILogger<PackageRequestService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<PackageRequest>> GetPackageRequestsAsync()
        {
            var response = await _httpClient.GetAsync("api/packagerequest");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<PackageRequest>>() ?? Array.Empty<PackageRequest>();
        }

        public async Task<PackageRequest> CreatePackageRequestAsync(PackageRequestForm form)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/packagerequest", form);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CreateOrderAsync Response: {Response}", responseContent);
                return await response.Content.ReadFromJsonAsync<PackageRequest>() ?? throw new InvalidOperationException("Failed to create order.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order.");
                throw;
            }
        }

        public async Task<PackageRequest> GetPackageRequestByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/packagerequest/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PackageRequest>() ?? throw new InvalidOperationException("Order not found.");
        }

        public Task<ProcessResponse> ProcessPackageRequestAsync(int uuid)
        {
            throw new NotImplementedException();
        }
    }
}
