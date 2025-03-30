# Azure AI Search Document Query System

Questa Web API ASP.NET Core consente di caricare documenti PDF, indicizzarli in Azure AI Search ed eseguire query semantiche per estrarre informazioni dai documenti.

## Funzionalità

- Upload di documenti PDF
- Estrazione del testo dai PDF utilizzando iText7
- Indicizzazione automatica in Azure AI Search
- Query semantiche sui documenti
- Interfaccia Swagger per testare l'API

## Requisiti

- .NET 8.0 SDK
- Un servizio Azure AI Search attivo
- Un servizio Azure Form Recognizer (opzionale)
- Un account Azure Storage (opzionale)

## Configurazione

Aggiorna il file `appsettings.json` con le tue credenziali Azure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureAISearch": {
    "Endpoint": "https://tuoservizio.search.windows.net",
    "IndexName": "documents-index",
    "AdminKey": "tua-chiave-admin",
    "QueryKey": "tua-chiave-query",
    "SemanticConfiguration": "default"
  },
  "AzureFormRecognizer": {
    "Endpoint": "https://tuoformrecognizer.cognitiveservices.azure.com/",
    "Key": "tua-chiave-form-recognizer"
  },
  "AzureBlobStorage": {
    "ConnectionString": "tua-stringa-connessione-storage",
    "ContainerName": "pdf-container"
  }
}
```

## Esecuzione

```bash
dotnet run
```

Accedi all'API Swagger all'indirizzo `https://localhost:5001/swagger` o `http://localhost:5000/swagger`.

## Endpoint API

- `POST /api/Documents/upload` - Carica un documento PDF
- `POST /api/Documents/search` - Cerca nei documenti
- `DELETE /api/Documents/{id}` - Elimina un documento

## Licenza

MIT
