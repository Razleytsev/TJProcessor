namespace TJConnector.StateSystem.Model.ExternalResponses.MarkingCode
{
    public class EmissionListResponse
    {
        public EmissionInfoResponse[]? items { get; set; }
        public int? total { get; set; }
        public int? statusCode { get; set; }
        public string? message { get; set; }

    }
}
