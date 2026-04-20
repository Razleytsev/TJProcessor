using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.SharedLibrary.DTOs.Forms;

public class BatchCreateForm
{
    public int CodesCount { get; set; }
    public sbyte Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public string User { get; set; } = string.Empty;
}