namespace TJConnector.Postgres.Entities
{
    public class PackageRequest
    {
        public int Id { get; set; }
        public string? Filename { get; set; }
        public string User { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public StatusHistory? StatusHistory { get; set; }

        public List<Package>? Packages { get; set; }
    }
}
