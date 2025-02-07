namespace TJConnector.StateSystem.Model.ExternalRequests.MarkingCode
{
    public class ApplicationCreateRequest
    {
        public DateTimeOffset? applicationDate { get; set; }
        public string[] codes { get; set; } = new string[0];
        public Guid factoryUuid { get; set; }
        public List<GroupCode> groupCodes { get; set; } = new List<GroupCode>();
        public Guid? locationUuid { get; set; }
        public Guid? markingLineUuid { get; set; }
        public sbyte result { get; set; } = 0;
        public sbyte type { get; set; } = 2;
    }
    public class GroupCode
    {
        public string groupCode { get; set; } = string.Empty;
        public string[] codes { get; set; } = new string[0];
    }
}
