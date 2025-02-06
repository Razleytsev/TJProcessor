using System.Net.Http.Json;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.Container;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.StateSystem.Services.Implementation
{
    public class ExternalContainerService(GetHttpClient client) : IExternalContainer
    {
        public async Task<CustomResult<ContainerInfoResponse>> ContainerInfo(string code)
        {
            var httpClient = client.GetPublicHttpClient();
            var result = await httpClient.GetFromJsonAsync<ContainerInfoResponse>($"container/{code}");

            return new CustomResult<ContainerInfoResponse>()
            {
                Content = result,
                Success = true
            };
        }

        public Task<CustomResult<List<ContainerInfoResponse>>> ContainerInfoList(ListRequestRequest body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<ContainerOperationInfoResponse>> ContainerOperation(ContainerOperationCreateRequest body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<ContainerRegisterResponse>> ContainerOperationCheck(string uuid)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<ContainerOperationListResponse>> ContainerOperationList(ListRequestRequest body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<ProcessResponse>> ContainerOperationProcess(ProcessDocument body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<ContainerRegisterResponse>> ContainerRegister(ContainerRegisterRequest codes)
        {
            throw new NotImplementedException();
        }
    }
}
