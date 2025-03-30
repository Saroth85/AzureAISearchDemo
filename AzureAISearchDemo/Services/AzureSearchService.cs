using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using AzureAISearchDemo.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureAISearchDemo.Services
{
    public class AzureSearchService : IAzureSearchService
    {
        private readonly SearchIndexClient _searchIndexClient;
        private readonly SearchClient _searchClient;
        private readonly ILogger<AzureSearchService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _indexName;

        public AzureSearchService(
            SearchIndexClient searchIndexClient,
            SearchClient searchClient,
            ILogger<AzureSearchService> logger,
            IConfiguration configuration)
        {
            _searchIndexClient = searchIndexClient;
            _searchClient = searchClient;
            _logger = logger;
            _configuration = configuration;
            _indexName = _configuration["AzureAISearch:IndexName"] ?? "documents-index";
        }

        public async Task<bool> CreateIndexIfNotExistsAsync()
        {
            try
            {
                Response<SearchIndex>? response = null;
                
                try 
                {
                    response = await _searchIndexClient.GetIndexAsync(_indexName);
                    _logger.LogInformation("Index {IndexName} already exists", _indexName);
                    return true;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    _logger.LogInformation("Index {IndexName} does not exist, creating it", _indexName);
                    // Index doesn't exist, we'll create it below
                }
                
                // Index doesn't exist, create it
                var fieldBuilder = new FieldBuilder();
                var searchFields = fieldBuilder.Build(typeof(DocumentModel));

                var definition = new SearchIndex(_indexName)
                {
                    Fields = searchFields
                };

                // Non includiamo le impostazioni semantiche perché non sono supportate nella versione attuale
                // Rimuovo tutto il blocco try-catch relativo a SemanticSettings

                await _searchIndexClient.CreateOrUpdateIndexAsync(definition);
                _logger.LogInformation("Created index {IndexName}", _indexName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating index {IndexName}", _indexName);
                return false;
            }
        }

        public async Task<bool> UploadDocumentAsync(DocumentModel document)
        {
            try
            {
                var batch = IndexDocumentsBatch.Upload(new[] { document });
                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
                
                if (result.Results.All(r => r.Succeeded))
                {
                    _logger.LogInformation("Document {Id} indexed successfully", document.Id);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to index document {Id}", document.Id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document {Id}", document.Id);
                return false;
            }
        }

        public async Task<List<SearchResult>> SearchDocumentsAsync(SearchRequest searchRequest)
        {
            try
            {
                SearchOptions options = new SearchOptions
                {
                    IncludeTotalCount = true,
                    Size = searchRequest.Top,
                };

                // Try to use semantic search if requested, but only basic functionality
                // as semantic queries require higher tier Azure Cognitive Search
                if (searchRequest.UseSemanticSearch)
                {
                    try
                    {
                        options.QueryType = SearchQueryType.Semantic;
                        
                        // Rimuovo la configurazione semantica che non è disponibile nella versione del pacchetto
                        // options.SemanticConfigurationName = _configuration["AzureAISearch:SemanticConfiguration"] ?? "default";
                        // options.SemanticSearch = new SemanticSearchOptions {...};
                        
                        _logger.LogInformation("Using semantic search");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not configure semantic search. Falling back to standard search.");
                        options.QueryType = SearchQueryType.Simple;
                    }
                }
                else
                {
                    options.QueryType = SearchQueryType.Simple;
                }

                options.Select.Add("Id");
                options.Select.Add("FileName");

                // Execute search
                var response = await _searchClient.SearchAsync<DocumentModel>(searchRequest.QueryText, options);
                var searchResults = new List<SearchResult>();

                // Uso direttamente la proprietà Results invece del metodo GetResultsAsync
                foreach (var result in response.Value.GetResults())
                {
                    var searchResult = new SearchResult
                    {
                        Id = result.Document.Id,
                        FileName = result.Document.FileName,
                        Score = result.Score ?? 0
                    };

                    // Extract captions if available - rimuovo il riferimento a SemanticSearch che non è disponibile
                    // in questa versione del pacchetto
                    /*
                    if (result.SemanticSearch?.Captions != null)
                    {
                        foreach (var caption in result.SemanticSearch.Captions)
                        {
                            searchResult.Excerpts.Add(caption.Text);
                        }
                    }
                    */
                    
                    searchResults.Add(searchResult);
                }

                return searchResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with query: {Query}", searchRequest.QueryText);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            try
            {
                // Corretto il secondo parametro, ora passando un array di stringhe contenente l'id
                var batch = IndexDocumentsBatch.Delete("Id", new[] { id });
                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
                
                return result.Results.All(r => r.Succeeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {Id}", id);
                return false;
            }
        }
    }
}
