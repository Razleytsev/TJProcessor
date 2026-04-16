using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.StateSystem.Model.ExternalDbResult
{
    public class DbContainerContent
    {
        public List<ContainerContent> Content { get; set; } = new List<ContainerContent>();
    }
    public class ContainerContent
    {
        public string Bundle { get; set; } = string.Empty;
        public string Pack { get; set; } = string.Empty;
    }
}
