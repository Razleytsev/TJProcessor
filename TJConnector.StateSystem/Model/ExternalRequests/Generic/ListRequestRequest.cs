using System.Text.Json.Serialization;

namespace TJConnector.StateSystem.Model.ExternalRequests.Generic
{
    public class ListRequestRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Filter? filters { get; set; }
        public int limit { get; set; } = 100;
        public int offset { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Order? order { get; set; }
    }

    public class Filter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid[]? factoryUuid { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? code { get; set; }
    }

    public class Order
    {
        public OrderCreatedAt? createdAt { get; set; }
        public OrderEntityStatus? status { get; set; }
        public OrderEntityType? type { get; set; }
    }
    public class OrderCreatedAt
    {
        public string dir { get; set; } = "DESC";
    }
    public class OrderEntityStatus
    {
        public string dir { get; set; } = "DESC";
    }
    public class OrderEntityType
    {
        public string dir { get; set; } = "DESC";
    }
}
