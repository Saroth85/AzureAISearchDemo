using AzureAISearchDemo.Services;
using Microsoft.Extensions.Azure;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

try
{
    // Add Azure AI Search client
    builder.Services.AddAzureClients(clientBuilder =>
    {
        // Verifica che le chiavi non siano null
        string endpoint = builder.Configuration["AzureAISearch:Endpoint"] 
            ?? throw new InvalidOperationException("AzureAISearch:Endpoint configuration is missing");
        string indexName = builder.Configuration["AzureAISearch:IndexName"] 
            ?? throw new InvalidOperationException("AzureAISearch:IndexName configuration is missing");
        string adminKey = builder.Configuration["AzureAISearch:AdminKey"] 
            ?? throw new InvalidOperationException("AzureAISearch:AdminKey configuration is missing");
        
        clientBuilder.AddSearchClient(
            new Uri(endpoint),
            indexName,
            new Azure.AzureKeyCredential(adminKey));
        
        clientBuilder.AddSearchIndexClient(
            new Uri(endpoint),
            new Azure.AzureKeyCredential(adminKey));
            
        // Add Form Recognizer client
        string formRecognizerEndpoint = builder.Configuration["AzureFormRecognizer:Endpoint"] 
            ?? throw new InvalidOperationException("AzureFormRecognizer:Endpoint configuration is missing");
        string formRecognizerKey = builder.Configuration["AzureFormRecognizer:Key"] 
            ?? throw new InvalidOperationException("AzureFormRecognizer:Key configuration is missing");
        
        clientBuilder.AddFormRecognizerClient(
            new Uri(formRecognizerEndpoint),
            new Azure.AzureKeyCredential(formRecognizerKey));
            
        // Add Blob Storage client
        string storageConnectionString = builder.Configuration["AzureBlobStorage:ConnectionString"] 
            ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString configuration is missing");
        
        clientBuilder.AddBlobServiceClient(storageConnectionString);
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Error configuring Azure clients: {ex.Message}");
    throw;
}

// Register services
builder.Services.AddSingleton<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddSingleton<IAzureSearchService, AzureSearchService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Azure AI Search Document API",
        Version = "v1",
        Description = "API for uploading and querying documents using Azure AI Search"
    });
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    else
    {
        Console.WriteLine($"Warning: XML documentation file not found at {xmlPath}");
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure AI Search Document API v1"));

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure uploads directory exists
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Uploads"));

app.Run();
