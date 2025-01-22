using System.Net.Http.Json;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Product;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.StateSystem.Services.Implementation
{
    public class ExternalEmissionService(GetHttpClient client) : IExternalEmission
    {
        public Task<CustomResult<DocumentCreateResponse>> CreateCodeApplication(ApplicationCreateRequest body)
        {
            throw new NotImplementedException();
        }

        public async Task<CustomResult<DocumentCreateResponse>> CreateCodeEmission(EmissionCreateRequest body)
        {
            var httpClient = client.GetPublicHttpClient();
            var response = await httpClient.PostAsJsonAsync("markingCode/emission", body);

            if (response.Content is null)
            {
                return new CustomResult<DocumentCreateResponse>() { Success = false, Message = "Execution error" };
            }

            var result = await response.Content.ReadFromJsonAsync<DocumentCreateResponse>();

            return new CustomResult<DocumentCreateResponse>()
            {
                Content = result,
                Success = true
            };
        }

        public async Task<CustomResult<EmissionCodesResponse>> GetCodesFromEmission(ProcessDocument body)
        {
            var httpClient = client.GetPublicHttpClient();
            var response = await httpClient.PostAsJsonAsync("markingCode/emission/codes", body);

            if (response.Content is null)
            {
                return new CustomResult<EmissionCodesResponse>() { Success = false, Message = "Execution error" };
            }

            var result = await response.Content.ReadFromJsonAsync<EmissionCodesResponse>();

            return new CustomResult<EmissionCodesResponse>()
            {
                Content = result,
                Success = true
            };
        }

        public async Task<CustomResult<EmissionInfoResponse>> GetEmissionInfo(Guid uuid)
        {
            var httpClient = client.GetPublicHttpClient();
            var response = await httpClient.GetFromJsonAsync<EmissionInfoResponse>($"markingCode/emission/{uuid}");

            if (response is null)
            {
                return new CustomResult<EmissionInfoResponse>() { Success = false, Message = "Execution error" };
            }

            return new CustomResult<EmissionInfoResponse>()
            {
                Content = response,
                Success = true
            };
        }

        public async Task<CustomResult<EmissionListResponse>> GetEmissionList(ListRequestRequest listRequestBody)
        {
            var httpClient = client.GetPublicHttpClient();
            var response = await httpClient.PostAsJsonAsync($"product/personal/find", listRequestBody);

            if (response.Content is null)
            {
                return new CustomResult<EmissionListResponse>() { Success = false, Message = "Execution error" };
            }

            var result = await response.Content.ReadFromJsonAsync<EmissionListResponse>();

            return new CustomResult<EmissionListResponse>()
            {
                Content = result,
                Success = true
            };
        }

        public Task<CustomResult<ProcessResponse>> ProcessCodeApplication(ProcessDocument body)
        {
            throw new NotImplementedException();
        }

        public async Task<CustomResult<ProcessResponse>> ProcessCodeEmission(ProcessDocument body)
        {
            var httpClient = client.GetPublicHttpClient();
            var response = await httpClient.PostAsJsonAsync("markingCode/emission/process", body);

            if (response.Content is null)
            {
                return new CustomResult<ProcessResponse>() { Success = false, Message = "Execution error" };
            }

            var result = await response.Content.ReadFromJsonAsync<ProcessResponse>();

            return new CustomResult<ProcessResponse>()
            {
                Content = result,
                Success = true
            };
        }
    }
}
