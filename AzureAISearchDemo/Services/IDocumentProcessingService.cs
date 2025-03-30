using AzureAISearchDemo.Models;
using Microsoft.AspNetCore.Http;

namespace AzureAISearchDemo.Services
{
    public interface IDocumentProcessingService
    {
        Task<(bool Success, string FilePath, string ErrorMessage)> SaveDocumentAsync(IFormFile file);
        
        Task<string> ExtractTextFromPdfAsync(string filePath);
        
        Task<DocumentModel> ProcessDocumentAsync(IFormFile file);
    }
}
