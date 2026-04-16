using TJConnector.Postgres.Entities;

namespace TJConnector.Api.TransitBatches;

public class ProcessBatch
{
    public int BatchId { get; set; }
}

public class CreateOrdersForBatch
{
    public Batch Batch { get; set; }
}

public class ProcessOrdersForBatch
{
    public Batch Batch { get; set; }
}
