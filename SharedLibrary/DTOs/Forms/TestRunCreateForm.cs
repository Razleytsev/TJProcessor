namespace TJConnector.SharedLibrary.DTOs.Forms
{
    public class TestRunCreateForm
    {
        public int PackProductId { get; set; }
        public int BundleProductId { get; set; }
        public int PacksPerBundle { get; set; }
        public int BundlesPerContainer { get; set; }
        public Guid FactoryUuid { get; set; }
        public Guid MarkingLineUuid { get; set; }
        public Guid LocationUuid { get; set; }
        public string User { get; set; } = string.Empty;
    }
}
