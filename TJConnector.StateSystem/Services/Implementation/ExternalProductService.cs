using System;
using System.Net.Http.Json;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Product;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.StateSystem.Services.Implementation
{
    public class ExternalProductService(GetHttpClient client) : IExternalProduct
    {
        public async Task<CustomResult<ProductInfoResponse>> GetProductByUUID(string uuid)
        {
            var httpClient = client.GetPublicHttpClient();
            var result = await httpClient.GetFromJsonAsync<ProductInfoResponse>($"product/{uuid}");

            if (result is null)
            {
                return new CustomResult<ProductInfoResponse>() { Success = false, Message = "Execution error" };
            }

            return new CustomResult<ProductInfoResponse>()
            {
                Content = result,
                Success = true
            };
        }

        public async Task<CustomResult<ProductPersonalListResponse>> GetProductPersonalList(ListRequestRequest listRequestBody)
        {
            var httpClient = client.GetPublicHttpClient();
            var response = await httpClient.PostAsJsonAsync($"product/personal/find", listRequestBody);

            if (response.Content is null)
            {
                return new CustomResult<ProductPersonalListResponse>() { Success = false, Message = "Execution error" };
            }

            var result = await response.Content.ReadFromJsonAsync<ProductPersonalListResponse>();

            return new CustomResult<ProductPersonalListResponse>()
            {
                Content = result,
                Success = true
            };
        }
    }
}
