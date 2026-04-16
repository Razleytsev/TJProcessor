using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJConnector.Postgres.Entities;
public class Batch
{
    public int Id { get; set; }
    public int Type { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int Count { get; set; }
    public int Status { get; set; } = 0;
    public string? Description { get; set; }
    public string? User { get; set; }
    public DateTimeOffset RecordDate { get; set; }
    public StatusHistory[] StatusHistoryJson { get; set; } = new StatusHistory[0];
    public List<CodeOrder>? CodeOrders { get; set; } = new List<CodeOrder>();
}