using TJConnector.StateSystem.Model.ExternalRequests.Container;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;

namespace TJConnector.StateSystem.Services.Contracts
{
    public interface IExternalEmission
    {
        Task<CustomResult<EmissionInfoResponse>> GetEmissionInfo(Guid uuid);
        Task<CustomResult<EmissionListResponse>> GetEmissionList(ListRequestRequest listRequestBody);

        Task<CustomResult<DocumentCreateResponse>> CreateCodeEmission(EmissionCreateRequest body);
        Task<CustomResult<ProcessResponse>> ProcessCodeEmission(ProcessDocument body);
        Task<CustomResult<EmissionCodesResponse>> GetCodesFromEmission(DownloadCodesRequest body);

        Task<CustomResult<EmissionInfoResponse>> GetContainerEmissionInfo(Guid uuid);
        Task<CustomResult<DocumentCreateResponse>> CreateContainerEmission(EmissionCreateRequest body);
        Task<CustomResult<DocumentCreateResponse>> CreateContainerEmissionMinimal(ContainerEmissionCreateRequest body);
        Task<CustomResult<ProcessResponse>> ProcessContainerEmission(ProcessDocument body);
        Task<CustomResult<EmissionCodesResponse>> GetCodesFromContainerEmission(DownloadCodesRequest body);


        Task<CustomResult<DocumentCreateResponse>> CreateCodeApplication(ApplicationCreateRequest body);
        Task<CustomResult<ProcessResponse>> ProcessCodeApplication(ProcessDocument body);
        Task<CustomResult<EmissionInfoResponse>> GetCodeApplicationInfo(Guid uuid);
    }
}
