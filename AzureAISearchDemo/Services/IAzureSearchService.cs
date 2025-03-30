using AzureAISearchDemo.Models;

namespace AzureAISearchDemo.Services
{
    public interface IAzureSearchService
    {
        Task<bool> CreateIndexIfNotExistsAsync();
        
        Task<bool> UploadDocumentAsync(DocumentModel document);
        
        Task<List<SearchResult>> SearchDocumentsAsync(SearchRequest searchRequest);
        
        Task<bool> DeleteDocumentAsync(string id);
    }
}
