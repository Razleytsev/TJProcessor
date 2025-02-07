using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    public class ExternalEmissionService : IExternalEmission
    {
        private readonly CustomHttpClient _httpClient;
        private readonly ILogger<ExternalEmissionService> _logger;

        public ExternalEmissionService(CustomHttpClient httpClient, ILogger<ExternalEmissionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CustomResult<EmissionInfoResponse>> GetEmissionInfo(Guid uuid)
        {
            if (uuid == Guid.Empty)
            {
                _logger.LogError("UUID cannot be empty.");
                return new CustomResult<EmissionInfoResponse> { Success = false, Message = "UUID is required." };
            }

            try
            {
                var response = await _httpClient.GetAsync($"markingCode/emission/{uuid}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to fetch emission info for UUID {uuid}. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<EmissionInfoResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<EmissionInfoResponse>();
                return new CustomResult<EmissionInfoResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching emission info for UUID {uuid}.");
                return new CustomResult<EmissionInfoResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<EmissionListResponse>> GetEmissionList(ListRequestRequest listRequestBody)
        {
            if (listRequestBody == null)
            {
                _logger.LogError("List request body cannot be null.");
                return new CustomResult<EmissionListResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("markingCode/emission/list", listRequestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to fetch emission list. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<EmissionListResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<EmissionListResponse>();
                return new CustomResult<EmissionListResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching emission list.");
                return new CustomResult<EmissionListResponse> { Success = false, Message = ex.Message };
            }
        }

        public async Task<CustomResult<DocumentCreateResponse>> CreateCodeEmission(EmissionCreateRequest body)
        {
            if (body == null)
            {
                _logger.LogError("Emission create request body cannot be null.");
                return new CustomResult<DocumentCreateResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("markingCode/emission", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to create code emission. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<DocumentCreateResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<DocumentCreateResponse>();
                return new CustomResult<DocumentCreateResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating code emission.");
                return new CustomResult<DocumentCreateResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<ProcessResponse>> ProcessCodeEmission(ProcessDocument body)
        {
            if (body == null)
            {
                _logger.LogError("Process document body cannot be null.");
                return new CustomResult<ProcessResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("markingCode/emission/process", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to process code emission. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<ProcessResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<Dictionary<Guid, ProcessMessage>>();
                var result2 = new ProcessResponse()
                {
                    ProcessResult = new Dictionary<Guid, ProcessMessage?>
                    {
                        {
                            result.FirstOrDefault().Key,
                            result.FirstOrDefault().Value
                        }
                    }
                };

                return new CustomResult<ProcessResponse> { Content = result2, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing code emission.");
                return new CustomResult<ProcessResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<EmissionCodesResponse>> GetCodesFromEmission(DownloadCodesRequest body)
        {
            if (body == null)
            {
                _logger.LogError("Process document body cannot be null.");
                return new CustomResult<EmissionCodesResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("markingCode/emission/codes", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to fetch codes from emission. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<EmissionCodesResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<EmissionCodesResponse>();
                return new CustomResult<EmissionCodesResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching codes from emission.");
                return new CustomResult<EmissionCodesResponse> { Success = false, Message = ex.Message };
            }
        }


        public async Task<CustomResult<EmissionInfoResponse>> GetContainerEmissionInfo(Guid uuid)
        {
            if (uuid == Guid.Empty)
            {
                _logger.LogError("UUID cannot be empty.");
                return new CustomResult<EmissionInfoResponse> { Success = false, Message = "UUID is required." };
            }

            try
            {
                var response = await _httpClient.GetAsync($"container/emission/{uuid}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to fetch emission info for UUID {uuid}. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<EmissionInfoResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<EmissionInfoResponse>();
                return new CustomResult<EmissionInfoResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching emission info for UUID {uuid}.");
                return new CustomResult<EmissionInfoResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<DocumentCreateResponse>> CreateContainerEmission(EmissionCreateRequest body)
        {
            if (body == null)
            {
                _logger.LogError("Emission create request body cannot be null.");
                return new CustomResult<DocumentCreateResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("container/emission", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to create code emission. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<DocumentCreateResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<DocumentCreateResponse>();
                return new CustomResult<DocumentCreateResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating code emission.");
                return new CustomResult<DocumentCreateResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<ProcessResponse>> ProcessContainerEmission(ProcessDocument body)
        {
            if (body == null)
            {
                _logger.LogError("Process document body cannot be null.");
                return new CustomResult<ProcessResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("container/emission/process", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to process code emission. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<ProcessResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<Dictionary<Guid, ProcessMessage>>();
                var result2 = new ProcessResponse()
                {
                    ProcessResult = new Dictionary<Guid, ProcessMessage?>
                    {
                        {
                            result.FirstOrDefault().Key,
                            result.FirstOrDefault().Value
                        }
                    }
                };

                return new CustomResult<ProcessResponse> { Content = result2, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing code emission.");
                return new CustomResult<ProcessResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<EmissionCodesResponse>> GetCodesFromContainerEmission(DownloadCodesRequest body)
        {
            if (body == null)
            {
                _logger.LogError("Process document body cannot be null.");
                return new CustomResult<EmissionCodesResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.GetAsync($"container/emission/{body.uuid}/codes");

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to fetch codes from emission. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<EmissionCodesResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<EmissionCodesResponse>();
                return new CustomResult<EmissionCodesResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching codes from emission.");
                return new CustomResult<EmissionCodesResponse> { Success = false, Message = ex.Message };
            }
        }

        public async Task<CustomResult<DocumentCreateResponse>> CreateCodeApplication(ApplicationCreateRequest body)
        {
            if (body == null)
            {
                _logger.LogError("Application create request body cannot be null.");
                return new CustomResult<DocumentCreateResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("markingCode/report/apply", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to create code application. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<DocumentCreateResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<DocumentCreateResponse>();
                return new CustomResult<DocumentCreateResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating code application.");
                return new CustomResult<DocumentCreateResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<ProcessResponse>> ProcessCodeApplication(ProcessDocument body)
        {
            if (body == null)
            {
                _logger.LogError("Process document body cannot be null.");
                return new CustomResult<ProcessResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("markingCode/report/apply/process", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to process code application. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<ProcessResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }
                string mmm = await response.Content.ReadAsStringAsync();
                string bo = body.ToString();
                var result = await response.Content.ReadFromJsonAsync<ProcessResponse>();
                return new CustomResult<ProcessResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing code application.");
                return new CustomResult<ProcessResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<EmissionInfoResponse>> GetCodeApplicationInfo(Guid uuid)
        {
            if (uuid == Guid.Empty)
            {
                _logger.LogError("UUID cannot be empty.");
                return new CustomResult<EmissionInfoResponse> { Success = false, Message = "UUID is required." };
            }

            try
            {
                var response = await _httpClient.GetAsync($"markingCode/report/apply/{uuid}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to fetch emission info for UUID {uuid}. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<EmissionInfoResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<EmissionInfoResponse>();
                return new CustomResult<EmissionInfoResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching emission info for UUID {uuid}.");
                return new CustomResult<EmissionInfoResponse> { Success = false, Message = ex.Message };
            }
        }
    }
}
