using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.StateSystem.Model.ExternalDbResult
{
    public class DbContainerStatus
    {
        public List<ContainerStatus> Content { get; set; } = new List<ContainerStatus>();
    }
    public class ContainerStatus
    {
        public string ExternalDbCode { get; set; } = string.Empty;
        public byte ExternalDbStatus { get; set; } = 0;
        public string ExternalDbStatusMessage { get; set; } = string.Empty;

    }
}
