using System.ComponentModel.DataAnnotations.Schema;

namespace TJConnector.Postgres.Entities
{
    public class CodeOrderContent
    {
        public int Id { get; set; }

        public int CodeOrderId { get; set; }
        public CodeOrder? CodeOrder { get; set; }
        public string[] OrderContent { get; set; } = [];
        public DateTimeOffset RecordDate { get; set; }
        public DownloadHistory? DownloadHistory { get; set; }
    }
    public class DownloadHistory
    {
        public DateTimeOffset DownloadTime { get; set; }
        public string User { get; set; } = string.Empty;
    }
}
