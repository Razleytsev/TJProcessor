namespace TJConnector.StateSystem.Model.ExternalResponses.Generic
{
    public class ProcessResponse
    {
        public Dictionary<Guid, ProcessMessage?>? ProcessResult { get; set; }
    }

    public class ProcessMessage
    {
        public ProcessError[]? errors { get; set; }
        public string? message { get; set; }
    }
    public class ProcessError
    {
        public string? code { get; set; }
        public string? message { get; set; }
        public int? statusCode { get; set; }
        public string? name { get; set; }
    }
}
