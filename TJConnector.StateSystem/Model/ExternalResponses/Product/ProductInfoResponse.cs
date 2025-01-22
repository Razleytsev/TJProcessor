namespace TJConnector.StateSystem.Model.ExternalResponses.Product
{

    public class ProductInfoResponse
    {
        public Guid id { get; set; }
        public Guid uuid { get; set; }
        public DateTime version { get; set; }
        public DateTime createdAt { get; set; }
        public sbyte status { get; set; }
        public sbyte type { get; set; }
        public string name { get; set; } = string.Empty;
        public string trademark { get; set; } = string.Empty;
        public string gtin { get; set; } = string.Empty;
        public int weightGross { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string manufacturerName { get; set; } = string.Empty;
        public string manufacturerAddress { get; set; } = string.Empty;
        public int packagingType { get; set; }
        public string packagingMaterial { get; set; } = string.Empty;
        public int dueDays { get; set; }
        public string storageConditions { get; set; } = string.Empty;
        public string composition { get; set; } = string.Empty;
        public Guid countryOfOriginUuid { get; set; }
        public int group { get; set; }
        public int category { get; set; }
        public float? nicotineContent { get; set; }
        public int tarContent { get; set; }
        public string[]? products { get; set; }
        public string tnved { get; set; } = string.Empty;
        public string gpc { get; set; } = string.Empty;
        public bool excise { get; set; }
        public string classifier { get; set; } = string.Empty;
        public string productCountryOfOrigin { get; set; } = string.Empty;
    }
}
