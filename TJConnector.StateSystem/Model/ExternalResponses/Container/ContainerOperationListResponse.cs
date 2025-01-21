namespace TJConnector.StateSystem.Model.ExternalResponses.Container
{
    public class ContainerOperationListResponse
    {
        public ContainerOperationInfoResponse[]? items { get; set; }
        public int? total { get; set; }
        public int? statusCode { get; set; }
        public string? message { get; set; }
    }
}
