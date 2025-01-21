using TJConnector.StateSystem.Model.ExternalRequests.Container;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;

namespace TJConnector.StateSystem.Services.Contracts
{
    public interface IExternalContainer
    {
        Task<CustomResult<ContainerRegisterResponse>> ContainerRegister(ContainerRegisterRequest codes);
        Task<CustomResult<ContainerOperationInfoResponse>> ContainerOperation(ContainerOperationCreateRequest body);
        Task<CustomResult<ProcessResponse>> ContainerOperationProcess(ProcessDocument body);
        Task<CustomResult<ContainerInfoResponse>> ContainerInfo(string code);
        Task<CustomResult<ContainerRegisterResponse>> ContainerOperationCheck(string uuid);
        Task<CustomResult<ContainerOperationListResponse>> ContainerOperationList(ListRequestRequest body);
    }
}
