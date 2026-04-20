namespace TJConnector.StateSystem.Model.ExternalResponses.Generic
{
    public class DocumentCreateResponse
    {
        public Guid? id { get; set; }
        public Guid? uuid { get; set; }
        public DateTime? version { get; set; }
        public int? statusCode { get; set; }
        public string? message { get; set; }
    }
}
