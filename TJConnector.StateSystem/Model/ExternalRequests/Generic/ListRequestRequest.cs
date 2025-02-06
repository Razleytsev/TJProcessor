using System.Text.Json.Serialization;

namespace TJConnector.StateSystem.Model.ExternalRequests.Generic
{
    public class ListRequestRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Filter? filter { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Order? order { get; set; }
    }

    public class Filter
    {
        public Guid[]? factoryUuid { get; set; }
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
