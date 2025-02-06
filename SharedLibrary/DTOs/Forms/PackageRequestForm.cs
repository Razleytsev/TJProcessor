using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJConnector.Postgres.Entities;

namespace TJConnector.SharedLibrary.DTOs.Forms
{
    public class PackageRequestForm
    {
        public string Description { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string? Filename { get; set; }
        public List<Link> packages { get; set; } = new List<Link>();
    }
    public class Link
    {
        public string Code { get; set; } = "";
        public string SSCCCode { get; set; } = "";
    }
}
