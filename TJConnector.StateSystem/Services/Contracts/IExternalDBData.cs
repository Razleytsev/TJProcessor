using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TJConnector.StateSystem.Model.ExternalDbResult;
using TJConnector.StateSystem.Model.ExternalResponses.Container;

namespace TJConnector.StateSystem.Services.Contracts
{
    public interface IExternalDBData
    {
        Task<CustomResult<DbContainerContent>> GetContainerContent(string containerId);
        Task<CustomResult<DbContainerStatus>> GetContainerInfo(List<string> containerId);
    }
}
