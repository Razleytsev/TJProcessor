namespace TJConnector.StateSystem.Model.ExternalResponses.MarkingCode
{
    public class EmissionInfoResponse
    {
        public int? category { get; set; }
        public int? codesCount { get; set; }
        public DateTime? createdAt { get; set; }
        public bool? excise { get; set; }
        public string? exciseStatus { get; set; }
        public string? factoryName { get; set; }
        public Guid? factoryUuid { get; set; }
        public DateTime? finishedAt { get; set; }
        public string? gpc { get; set; }
        public sbyte? group { get; set; }
        public string? gtin { get; set; }
        public Guid id { get; set; }
        public string? markingLineName { get; set; }
        public string? markingLineUuid { get; set; }
        public string? name { get; set; }
        public string? ownerEmployerIdNumber { get; set; }
        public string? ownerName { get; set; }
        public string? ownerTaxIdNumber { get; set; }
        public Guid? ownerUuid { get; set; }
        public sbyte? productStatus { get; set; }
        public sbyte? productType { get; set; }
        public string? productUuid { get; set; }
        public sbyte? status { get; set; }
        public string? tnved { get; set; }
        public sbyte? type { get; set; }
        public int? unusedCodesCount { get; set; }
        public Guid? uuid { get; set; }
        public DateTime? version { get; set; }
        public int? statusCode { get; set; }
        public string? message { get; set; }
    }

}
