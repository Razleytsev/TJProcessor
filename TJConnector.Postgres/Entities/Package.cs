namespace TJConnector.Postgres.Entities
{
    public class Package
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string SSCCCode { get; set; } = string.Empty;
        public int Status { get; set; }
        public List<PackageContent>? Content { get; set; }
        public Guid? ContentApplicationGuid { get; set; }
        public Guid? AggregationGuid { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public StatusHistory[]? StatusHistory { get; set; } = new StatusHistory[0];
        public string? Comment { get; set; } = string.Empty;
        public int PackageRequestId { get; set; }

        public void AddStatus(int status)
        {
            StatusHistory = (StatusHistory ?? Array.Empty<StatusHistory>())
                .Append(new StatusHistory { Status = status, StatusDate = DateTimeOffset.UtcNow })
                .ToArray();
        }
    }
    public class PackageContent
    {
        public string Bundle { get; set; } = string.Empty;
        public string[] Packs { get; set; } = new string[0];
    }
}
