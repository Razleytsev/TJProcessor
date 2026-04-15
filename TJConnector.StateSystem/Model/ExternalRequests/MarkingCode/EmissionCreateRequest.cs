using System.Text.Json.Serialization;

namespace TJConnector.StateSystem.Model.ExternalRequests.MarkingCode
{
    public class EmissionCreateRequest
    {
        public int codesCount { get; set; }
        public Guid? productUuid { get; set; }
        public Guid? markingLineUuid { get; set; }
        public Guid? factoryUuid { get; set; }
        public sbyte Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? format { get; set; }
    }
}
