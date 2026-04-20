using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.StateSystem.Model.ExternalResponses.Generic
{
    public class ExternalApiErrorResponse
    {
        public int? statusCode { get; set; }
        public string? message { get; set; }
    }
}
