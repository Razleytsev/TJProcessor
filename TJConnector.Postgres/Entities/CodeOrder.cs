namespace TJConnector.Postgres.Entities
{
    public class CodeOrder
    {
        public int Id { get; set; }
        public int Type { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Count { get; set; }
        public int Status { get; set; }
        public Guid ExternalGuid { get; set; }
        public string? Description { get; set; }
        public string? User { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public StatusHistory? StatusHistory { get; set; }

        public CodeOrderContent? Content { get; set; }
    }
    public class StatusHistory
    {
        public int Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
    }
}
