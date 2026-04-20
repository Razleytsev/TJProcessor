using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.StateSystem.Model.ExternalResponses.Generic
{
    public class ListResponse<T>
    {
        public List<T>? items { get; set; }
        public int? total { get; set; }
        public int? statusCode { get; set; }
        public string? message { get; set; }
    }
}
