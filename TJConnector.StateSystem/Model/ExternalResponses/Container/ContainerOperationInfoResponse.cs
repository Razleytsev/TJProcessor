namespace TJConnector.StateSystem.Model.ExternalResponses.Container
{

    public class ContainerOperationInfoResponse
    {
        public string[]? codes { get; set; }
        public int codesCount { get; set; }
        public string containerCode { get; set; } = string.Empty;
        public sbyte containerType { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime? finishedAt { get; set; }
        public Guid id { get; set; }
        public string? locationName { get; set; }
        public string? locationUuid { get; set; }
        public string? ownerEmployerIdNumber { get; set; }
        public string? ownerName { get; set; }
        public string? ownerTaxIdNumber { get; set; }
        public string? ownerUuid { get; set; }
        public sbyte status { get; set; }
        public object[]? transferCodes { get; set; }
        public sbyte type { get; set; }
        public Guid uuid { get; set; }
        public DateTime version { get; set; }
    }

}
