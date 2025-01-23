using System.Net.Http;
using TJConnector.Postgres.Entities;
using TJConnector.Web.Services.Contracts;

namespace TJConnector.Web.Services.Implementation
{
    public class MetadataService : IMetadataService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderService> _logger;

        public MetadataService(HttpClient httpClient, ILogger<OrderService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<Factory>> GetFactoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/order");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<Factory>>() ?? Array.Empty<Factory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch orders.");
                throw;
            }
        }

        public async Task<IEnumerable<Location>> GetLocationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/order");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<Location>>() ?? Array.Empty<Location>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch orders.");
                throw;
            }
        }

        public async Task<IEnumerable<MarkingLine>> GetMarkingLinesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/order");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<MarkingLine>>() ?? Array.Empty<MarkingLine>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch orders.");
                throw;
            }
        }
    }
}
