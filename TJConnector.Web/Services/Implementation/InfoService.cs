using TJConnector.Api.Services;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.Web.Services.Contracts;

namespace TJConnector.Web.Services.Implementation
{
    public class InfoService : IInfoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InfoService> _logger;
        public InfoService(HttpClient httpClient, ILogger<InfoService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ListResponse<ContainerInfoResponse>> CisInfoList(string[] ids)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("external/code/find", ids);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CreateOrderAsync Response: {Response}", responseContent);
                var result = await response.Content.ReadFromJsonAsync<ListResponse<ContainerInfoResponse>>();

                foreach (string id in ids)
                {
                    if(result?.items?.Find(x => x.code == id) == null)
                        result?.items?.Add(new ContainerInfoResponse { code = id });
                }


                return result
                    ?? throw new InvalidOperationException("Failed to fetch info");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch info");
                throw;
            }
        }
    }
}
