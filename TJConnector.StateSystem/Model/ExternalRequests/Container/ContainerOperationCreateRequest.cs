namespace TJConnector.StateSystem.Model.ExternalRequests.Container
{
    public class ContainerOperationCreateRequest
    {
        public string[] codes { get; set; } = [];
        public string? containerCode { get; set; }
        public Guid locationUuid { get; set; }
        public string[] transferCodes { get; set; } = [];
        public sbyte type { get; set; } = 0;
    }
}
