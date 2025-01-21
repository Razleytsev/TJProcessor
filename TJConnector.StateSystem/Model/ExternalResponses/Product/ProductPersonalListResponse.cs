namespace TJConnector.StateSystem.Model.ExternalResponses.Product
{

    public class ProductPersonalListResponse
    {
        public ProductInfoResponse[]? items { get; set; }
        public int? total { get; set; }
        public int? statusCode { get; set; }
        public string? message { get; set; }
    }
}
