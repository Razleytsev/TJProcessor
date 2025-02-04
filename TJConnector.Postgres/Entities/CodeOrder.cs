namespace TJConnector.Postgres.Entities
{
    public class CodeOrder
    {
        public int Id { get; set; }
        public int Type { get; set; }

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        public int Count { get; set; }
        public int Status { get; set; } = 0;
        public Guid? ExternalGuid { get; set; }
        public string? Description { get; set; }
        public string? User { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public StatusHistory[] StatusHistoryJson { get; set; } = new StatusHistory[0];
        public string? StatusMessage { get; set; }

        public CodeOrderContent? Content { get; set; }
    }
    public class StatusHistory
    {
        public int Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
    }
}