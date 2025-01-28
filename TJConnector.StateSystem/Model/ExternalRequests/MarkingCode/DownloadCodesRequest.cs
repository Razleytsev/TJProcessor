using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.StateSystem.Model.ExternalRequests.MarkingCode
{
    public class DownloadCodesRequest
    {
        public int type { get; set; }
        public Guid uuid { get; set; }
    }
}
