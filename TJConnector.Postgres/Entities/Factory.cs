namespace TJConnector.Postgres.Entities
{
    public class Factory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ExternalUid { get; set; }
    }
}
