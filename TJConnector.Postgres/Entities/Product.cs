namespace TJConnector.Postgres.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public string Gtin { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid ExternalUid { get; set; }
        public DateTimeOffset RecordDate { get; set; }

        public List<CodeOrder>? CodeOrders { get; set; }
    }
}
