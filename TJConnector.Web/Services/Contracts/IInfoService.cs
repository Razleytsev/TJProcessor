using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;

namespace TJConnector.Web.Services.Contracts
{
    public interface IInfoService
    {
        Task<ListResponse<ContainerInfoResponse>> CisInfoList(string[] ids);
    }
}
