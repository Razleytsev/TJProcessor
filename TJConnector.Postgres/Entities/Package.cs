namespace TJConnector.Postgres.Entities
{
    public class Package
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string SSCCCode { get; set; } = string.Empty;
        public int Status { get; set; }
        public PackageContent? Content { get; set; }
        public Guid? ContentApplicationGuid { get; set; }
        public Guid? AggregationGuid { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public StatusHistory? StatusHistory { get; set; }

        public int PackageRequestId { get; set; }
        public PackageRequest? PackageRequest { get; set; }
    }
    public class PackageContent
    {
        public string Bundle { get; set; } = string.Empty;
        public List<string> Packs { get; set; } = new List<string>();
    }
}
