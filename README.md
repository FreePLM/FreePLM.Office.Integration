# FreePLM.Office.Integration

ASP.NET Core Web API backend service that provides PLM functionality for all Office add-ins (Word, Excel, PowerPoint, etc.).

## Architecture

```
Office Add-ins (.NET 4.8)
    ↓ HTTP/REST
FreePLM.Office.Integration (ASP.NET Core Web API - .NET 10)
    ↓
FreePLM.Common → FreePLM.Core → FreePLM.Base
    ↓
SQLite Database + Local File Storage
```

## Technology Stack

- **.NET 10** with C# 14
- **ASP.NET Core Web API**
- **Serilog** for logging (from FreePLM.Base)
- **Dapper** for data access (from FreePLM.Base)
- **Swagger/OpenAPI** for API documentation
- **SQLite** for database
- **Dependency Injection** with extension methods

## Configuration

### appsettings.json

```json
{
  "FreePLM": {
    "Server": {
      "Port": 5000,
      "AllowedOrigins": ["http://localhost"]
    },
    "Storage": {
      "Provider": "LocalDirectory",
      "LocalDirectory": {
        "RootPath": "C:\\PLM-Storage",
        "CreateIfNotExists": true
      }
    },
    "Database": {
      "Provider": "SQLite",
      "SQLite": {
        "ConnectionString": "Data Source=C:\\PLM-Data\\freeplm.db",
        "CreateIfNotExists": true
      }
    }
  }
}
```

## API Endpoints

### Health Check
- `GET /api/health` - Service health check

### Documents
- `GET /api/documents/{objectId}` - Get document metadata
- `GET /api/documents/{objectId}/content` - Download file
- `POST /api/documents` - Create new document
- `GET /api/documents/search` - Search documents

### CheckOut/CheckIn
- `POST /api/checkout` - Check out document
- `POST /api/checkout/checkin` - Check in document
- `POST /api/checkout/cancel` - Cancel checkout
- `GET /api/checkout/{objectId}/status` - Get lock status

### Workflow
- `POST /api/workflow/status` - Change document status
- `GET /api/workflow/{objectId}/history` - Get status history

## Running the Service

### Option 1: Visual Studio
1. Set `FreePLM.Office.Integration` as startup project
2. Press F5 to run
3. Swagger UI opens at `http://localhost:5000`

### Option 2: Command Line
```bash
cd C:\GitHubFreePLM\FreePLM.Office.Integration
dotnet run
```

### Option 3: Console App
```bash
dotnet run --project FreePLM.Office.Integration.csproj
```

## Logging

Logs are written to:
- **Console** - Real-time output
- **File** - `logs/freeplm-{date}.txt` (daily rolling)

Configured via Serilog from FreePLM.Base.

## Dependency Injection

Services are registered via extension methods in `Extensions/ServiceCollectionExtensions.cs`:

```csharp
builder.Services.AddFreePLMServices(builder.Configuration);
builder.Services.AddFreePLMCors(builder.Configuration);
```

## Current Status

✅ **Complete:**
- Web API project structure
- Configuration (appsettings.json)
- Serilog logging
- Swagger/OpenAPI documentation
- All REST API endpoints (with mock data)
- CORS configuration
- Dependency injection setup

⏳ **Next Steps:**
- Implement actual services in FreePLM.Common
- Implement repositories in FreePLM.Base
- Implement SQLite database
- Implement local file storage
- Add transaction management

## Testing with Word Add-in

1. Start the service:
   ```bash
   dotnet run
   ```

2. Service starts on `http://localhost:5000`

3. Open Word with FreePLM add-in installed

4. Test the connection:
   - Click "Check Out" button
   - Should see mock response instead of "service unavailable"

5. View API documentation:
   - Navigate to `http://localhost:5000`
   - Swagger UI shows all endpoints

## Project Dependencies

```
FreePLM.Office.Integration
    └── FreePLM.Common
        └── FreePLM.Core
            └── FreePLM.Base (includes Serilog, Dapper, etc.)
```

## Port Configuration

Default port: **5000**

Change in `appsettings.json`:
```json
{
  "FreePLM": {
    "Server": {
      "Port": 5000
    }
  }
}
```

## CORS Configuration

Configured to allow requests from Office add-ins running on localhost.

Add more origins in `appsettings.json`:
```json
{
  "FreePLM": {
    "Server": {
      "AllowedOrigins": [
        "http://localhost",
        "http://localhost:3000"
      ]
    }
  }
}
```
