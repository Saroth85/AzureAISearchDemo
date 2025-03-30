using AzureAISearchDemo.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;

namespace AzureAISearchDemo.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly string _uploadDirectory;

        public DocumentProcessingService(ILogger<DocumentProcessingService> logger)
        {
            _logger = logger;
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<(bool Success, string FilePath, string ErrorMessage)> SaveDocumentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, string.Empty, "File is empty");
            }

            if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
            {
                return (false, string.Empty, "Only PDF files are supported");
            }

            try
            {
                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string filePath = Path.Combine(_uploadDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return (true, filePath, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving document");
                return (false, string.Empty, $"Error saving document: {ex.Message}");
            }
        }

        public async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            var textBuilder = new StringBuilder();

            try
            {
                await Task.Run(() =>
                {
                    using (var pdfReader = new PdfReader(filePath))
                    using (var pdfDocument = new PdfDocument(pdfReader))
                    {
                        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                        {
                            var page = pdfDocument.GetPage(i);
                            ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                            string pageContent = PdfTextExtractor.GetTextFromPage(page, strategy);
                            textBuilder.AppendLine(pageContent);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                textBuilder.AppendLine($"Error extracting text: {ex.Message}");
            }

            return textBuilder.ToString();
        }

        public async Task<DocumentModel> ProcessDocumentAsync(IFormFile file)
        {
            var saveResult = await SaveDocumentAsync(file);
            
            if (!saveResult.Success)
            {
                throw new InvalidOperationException(saveResult.ErrorMessage);
            }

            string extractedText = await ExtractTextFromPdfAsync(saveResult.FilePath);

            var document = new DocumentModel
            {
                FileName = file.FileName,
                FileType = "PDF",
                Content = extractedText,
                UploadedDate = DateTimeOffset.Now,
                FileSizeInBytes = file.Length
            };

            return document;
        }
    }
}
