using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace AzureAISearchDemo.Models
{
    public class DocumentModel
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string FileName { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string FileType { get; set; } = string.Empty;

        [SearchableField(AnalyzerName = "en.lucene")]
        public string Content { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset UploadedDate { get; set; } = DateTimeOffset.Now;

        [SimpleField(IsFilterable = true)]
        public long FileSizeInBytes { get; set; }
    }

    public class SearchRequest
    {
        public string QueryText { get; set; } = string.Empty;
        public bool UseSemanticSearch { get; set; } = true;
        public int Top { get; set; } = 10;
    }

    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public double Score { get; set; }
        public List<string> Excerpts { get; set; } = new();
    }

    public class DocumentUploadResult
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
