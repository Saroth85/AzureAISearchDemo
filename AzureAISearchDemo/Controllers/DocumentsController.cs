using Microsoft.AspNetCore.Mvc;
using AzureAISearchDemo.Services;
using AzureAISearchDemo.Models;

namespace AzureAISearchDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly IAzureSearchService _azureSearchService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentProcessingService documentProcessingService,
            IAzureSearchService azureSearchService,
            ILogger<DocumentsController> logger)
        {
            _documentProcessingService = documentProcessingService;
            _azureSearchService = azureSearchService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a PDF document to be indexed
        /// </summary>
        /// <param name="file">PDF file to upload</param>
        /// <returns>Result of document upload and indexing</returns>
        /// <response code="200">Document uploaded and indexed successfully</response>
        /// <response code="400">Invalid file format or empty file</response>
        /// <response code="500">Error processing or indexing the document</response>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(DocumentUploadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
            {
                return BadRequest("Only PDF files are supported");
            }

            try
            {
                // Ensure index exists
                await _azureSearchService.CreateIndexIfNotExistsAsync();

                // Process document
                var document = await _documentProcessingService.ProcessDocumentAsync(file);
                
                // Index document
                bool indexed = await _azureSearchService.UploadDocumentAsync(document);

                if (indexed)
                {
                    return Ok(new DocumentUploadResult
                    {
                        Id = document.Id,
                        FileName = document.FileName,
                        Success = true,
                        Message = "Document uploaded and indexed successfully"
                    });
                }
                else
                {
                    return StatusCode(500, new DocumentUploadResult
                    {
                        FileName = document.FileName,
                        Success = false,
                        Message = "Document was processed but could not be indexed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {FileName}", file.FileName);
                return StatusCode(500, new DocumentUploadResult
                {
                    FileName = file.FileName,
                    Success = false,
                    Message = $"Error processing document: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Search documents with text query
        /// </summary>
        /// <param name="searchRequest">Search parameters</param>
        /// <returns>Search results</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Invalid search request</response>
        /// <response code="500">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(List<SearchResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchDocuments([FromBody] SearchRequest searchRequest)
        {
            if (string.IsNullOrWhiteSpace(searchRequest.QueryText))
            {
                return BadRequest("Search query cannot be empty");
            }

            try
            {
                var results = await _azureSearchService.SearchDocumentsAsync(searchRequest);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents");
                return StatusCode(500, $"Error searching documents: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a document from the index by ID
        /// </summary>
        /// <param name="id">Document ID to delete</param>
        /// <returns>Result of deletion operation</returns>
        /// <response code="200">Document deleted successfully</response>
        /// <response code="404">Document not found</response>
        /// <response code="500">Error deleting document</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            try
            {
                bool result = await _azureSearchService.DeleteDocumentAsync(id);
                
                if (result)
                {
                    return Ok($"Document {id} deleted successfully");
                }
                else
                {
                    return NotFound($"Document {id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {Id}", id);
                return StatusCode(500, $"Error deleting document: {ex.Message}");
            }
        }
    }
}
