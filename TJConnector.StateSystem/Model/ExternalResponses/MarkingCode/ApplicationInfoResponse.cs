using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.StateSystem.Model.ExternalResponses.MarkingCode
{

    public class ApplicationInfoResponse
    {
        public DateTimeOffset? applicationDate { get; set; }
        public object[] codes { get; set; }
        public int codesCount { get; set; }
        public DateTime createdAt { get; set; }
        public string factoryName { get; set; }
        public string factoryUuid { get; set; }
        public DateTime finishedAt { get; set; }
        public Groupcode[] groupCodes { get; set; }
        public string id { get; set; }
        public string locationName { get; set; }
        public string locationUuid { get; set; }
        public string markingLineName { get; set; }
        public string markingLineUuid { get; set; }
        public string ownerEmployerIdNumber { get; set; }
        public string ownerName { get; set; }
        public string ownerTaxIdNumber { get; set; }
        public string ownerUuid { get; set; }
        public DateTime productionDate { get; set; }
        public int result { get; set; }
        public int status { get; set; }
        public int type { get; set; }
        public string uuid { get; set; }
        public DateTime version { get; set; }
    }

    public class Groupcode
    {
        public string[] codes { get; set; }
        public string groupCode { get; set; }
    }

}
