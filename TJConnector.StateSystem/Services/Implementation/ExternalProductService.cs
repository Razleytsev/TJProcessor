using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly;
using System;
using System.Net.Http.Json;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Product;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.StateSystem.Services.Implementation
{
    public class ExternalProductService : IExternalProduct
    {
        private readonly CustomHttpClient _httpClient;
        private readonly ILogger<ExternalProductService> _logger;

        public ExternalProductService(CustomHttpClient httpClient, ILogger<ExternalProductService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CustomResult<ProductInfoResponse>> GetProductByUUID(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
            {
                _logger.LogError("UUID cannot be null or empty.");
                return new CustomResult<ProductInfoResponse> { Success = false, Message = "UUID is required." };
            }

            try
            {
                var response = await _httpClient.GetAsync($"product/{uuid}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch product with UUID {uuid}. Status code: {response.StatusCode}");
                    return new CustomResult<ProductInfoResponse> { Success = false, Message = $"HTTP error: {response.StatusCode}" };
                }

                var result = await response.Content.ReadFromJsonAsync<ProductInfoResponse>();
                return new CustomResult<ProductInfoResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching product with UUID {uuid}.");
                return new CustomResult<ProductInfoResponse> { Success = false, Message = ex.Message };
            }
        }

        public async Task<CustomResult<ProductPersonalListResponse>> GetProductPersonalList(ListRequestRequest listRequestBody)
        {
            if (listRequestBody == null)
            {
                _logger.LogError("List request body cannot be null.");
                return new CustomResult<ProductPersonalListResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("product/personal/find", listRequestBody);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch product list. Status code: {response.StatusCode}");
                    return new CustomResult<ProductPersonalListResponse> { Success = false, Message = $"HTTP error: {response.StatusCode}" };
                }

                var result = await response.Content.ReadFromJsonAsync<ProductPersonalListResponse>();
                return new CustomResult<ProductPersonalListResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching product list.");
                return new CustomResult<ProductPersonalListResponse> { Success = false, Message = ex.Message };
            }
        }
    }
}