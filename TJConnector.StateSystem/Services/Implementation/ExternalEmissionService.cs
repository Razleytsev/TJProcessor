using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.StateSystem.Services.Implementation
{
    public class ExternalEmissionService : IExternalEmission
    {
        public Task<CustomResult<DocumentCreateResponse>> CreateCodeApplication(ApplicationCreateRequest body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<DocumentCreateResponse>> CreateCodeEmission(EmissionCreateRequest body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<EmissionCodesResponse>> GetCodesFromEmission(ProcessDocument body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<EmissionInfoResponse>> GetEmissionInfo(Guid uuid)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<EmissionListResponse>> GetEmissionList()
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<ProcessResponse>> ProcessCodeApplication(ProcessDocument body)
        {
            throw new NotImplementedException();
        }

        public Task<CustomResult<ProcessResponse>> ProcessCodeEmission(ProcessDocument body)
        {
            throw new NotImplementedException();
        }
    }
}
