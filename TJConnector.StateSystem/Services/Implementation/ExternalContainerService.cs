using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.Container;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.StateSystem.Services.Implementation
{
    public class ExternalContainerService : IExternalContainer
    {
        private readonly CustomHttpClient _httpClient;
        private readonly ILogger<ExternalEmissionService> _logger;

        public ExternalContainerService(CustomHttpClient httpClient, ILogger<ExternalEmissionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }


        public async Task<CustomResult<ContainerInfoResponse>> ContainerInfo(string code)
        {
            if (code == null)
            {
                _logger.LogError("code cannot be empty.");
                return new CustomResult<ContainerInfoResponse> { Success = false, Message = "code is required." };
            }

            try
            {
                var response = await _httpClient.GetAsync($"cotnainer/{code}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to fetch code info for code {code}. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<ContainerInfoResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<ContainerInfoResponse>();
                return new CustomResult<ContainerInfoResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching info for code {code}.");
                return new CustomResult<ContainerInfoResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<ContainerOperationInfoResponse>> ContainerOperation(ContainerOperationCreateRequest body)
        {
            if (body == null)
            {
                _logger.LogError("Emission create request body cannot be null.");
                return new CustomResult<ContainerOperationInfoResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("container/report/operation", body);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ExternalApiErrorResponse>();
                    _logger.LogError($"Failed to create code report. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<ContainerOperationInfoResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<ContainerOperationInfoResponse>();
                return new CustomResult<ContainerOperationInfoResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating code emission.");
                return new CustomResult<ContainerOperationInfoResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<ContainerOperationInfoResponse>> ContainerOperationCheck(Guid uuid)
        {
            if (uuid == Guid.Empty)
            {
                _logger.LogError("UUID cannot be empty.");
                return new CustomResult<ContainerOperationInfoResponse> { Success = false, Message = "UUID is required." };
            }

            try
            {
                var response = await _httpClient.GetAsync($"container/report/operation/{uuid}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ContainerOperationInfoResponse>();
                    _logger.LogError($"Failed to fetch application info for UUID {uuid}. Status code: {response.StatusCode}, Message: {errorResponse?.message}");
                    return new CustomResult<ContainerOperationInfoResponse> { Success = false, Message = errorResponse?.message, StatusCode = errorResponse?.statusCode };
                }

                var result = await response.Content.ReadFromJsonAsync<ContainerOperationInfoResponse>();
                return new CustomResult<ContainerOperationInfoResponse> { Content = result, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching application info for UUID {uuid}.");
                return new CustomResult<ContainerOperationInfoResponse> { Success = false, Message = ex.Message };
            }
        }
        public async Task<CustomResult<ProcessResponse>> ContainerOperationProcess(ProcessDocument body)
        {
            if (body == null)
            {
                _logger.LogError("Process document body cannot be null.");
                return new CustomResult<ProcessResponse> { Success = false, Message = "Request body is required." };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("container/report/operation/process", body);

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




        public Task<CustomResult<ContainerOperationListResponse>> ContainerOperationList(ListRequestRequest body)
        {
            throw new NotImplementedException();
        }
        public Task<CustomResult<List<ContainerInfoResponse>>> ContainerInfoList(ListRequestRequest body)
        {
            throw new NotImplementedException();
        }
        public Task<CustomResult<ContainerRegisterResponse>> ContainerRegister(ContainerRegisterRequest codes)
        {
            throw new NotImplementedException();
        }
    }
}
