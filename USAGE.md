# DBAccess.Api Usage Guide

## Quick Start

### 1. Setup Database Connection

Update `appsettings.json` with your database connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SampleDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

For environment-specific settings, use `appsettings.Development.json` or environment variables.

### 2. Create Sample Database (Optional)

Use the included `SampleDatabase.sql` script to create a test database with sample data, stored procedures, and functions.

### 3. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

## API Usage Examples

### Using Stored Procedures

#### Example 1: Execute with Parameters
```bash
curl -X POST http://localhost:5000/api/storedprocedure/execute \
  -H "Content-Type: application/json" \
  -d '{
    "name": "sp_GetUsers",
    "parameters": {
      "UserId": 1,
      "Status": "Active"
    }
  }'
```

#### Example 2: Execute without Parameters
```bash
curl -X POST http://localhost:5000/api/storedprocedure/execute \
  -H "Content-Type: application/json" \
  -d '{
    "name": "sp_GetAllUsers"
  }'
```

### Using Database Functions

#### Example 1: Scalar Function
```bash
curl -X POST http://localhost:5000/api/function/execute \
  -H "Content-Type: application/json" \
  -d '{
    "name": "fn_CalculateTotal",
    "parameters": {
      "Amount": 100,
      "TaxRate": 0.15
    }
  }'
```

Response:
```json
{
  "success": true,
  "message": "Function executed successfully",
  "data": [
    {
      "result": 115.00
    }
  ],
  "rowsAffected": null
}
```

### Using Table Queries

#### Example 1: Simple Query
```bash
curl -X POST http://localhost:5000/api/table/query \
  -H "Content-Type: application/json" \
  -d '{
    "tableName": "Users",
    "top": 10
  }'
```

#### Example 2: Query with WHERE Clause
```bash
curl -X POST http://localhost:5000/api/table/query \
  -H "Content-Type: application/json" \
  -d '{
    "tableName": "Users",
    "whereClause": "Status = @Status",
    "parameters": {
      "Status": "Active"
    }
  }'
```

#### Example 3: Query with Filtering and Ordering
```bash
curl -X POST http://localhost:5000/api/table/query \
  -H "Content-Type: application/json" \
  -d '{
    "tableName": "Users",
    "whereClause": "Status = @Status AND CreatedDate > @Date",
    "parameters": {
      "Status": "Active",
      "Date": "2024-01-01"
    },
    "top": 10,
    "orderBy": "CreatedDate DESC"
  }'
```

## Using with Different Programming Languages

### C# / .NET
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
var request = new
{
    name = "sp_GetUsers",
    parameters = new Dictionary<string, object>
    {
        { "Status", "Active" }
    }
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync(
    "http://localhost:5000/api/storedprocedure/execute", 
    content
);
var result = await response.Content.ReadAsStringAsync();
```

### Python
```python
import requests

url = "http://localhost:5000/api/storedprocedure/execute"
data = {
    "name": "sp_GetUsers",
    "parameters": {
        "Status": "Active"
    }
}

response = requests.post(url, json=data)
print(response.json())
```

### JavaScript / Node.js
```javascript
const axios = require('axios');

const response = await axios.post(
  'http://localhost:5000/api/storedprocedure/execute',
  {
    name: 'sp_GetUsers',
    parameters: {
      Status: 'Active'
    }
  }
);

console.log(response.data);
```

### PowerShell
```powershell
$body = @{
    name = "sp_GetUsers"
    parameters = @{
        Status = "Active"
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/storedprocedure/execute" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

## Error Handling

The API returns standard HTTP status codes:
- **200 OK**: Successful execution
- **400 Bad Request**: Invalid request or execution error

Error response format:
```json
{
  "success": false,
  "message": "Error description",
  "data": null,
  "rowsAffected": null
}
```

## Security Best Practices

1. **Use Parameterized Queries**: All inputs are parameterized to prevent SQL injection
2. **Validate Input**: The API validates table names and procedure names
3. **Connection Strings**: Use environment variables or secure vaults in production
4. **Authentication**: Add authentication middleware (JWT, OAuth) for production
5. **Authorization**: Implement role-based access control for sensitive operations
6. **Rate Limiting**: Consider adding rate limiting to prevent abuse
7. **HTTPS**: Always use HTTPS in production environments

## Performance Tips

1. **Connection Pooling**: The API uses connection pooling by default
2. **Async Operations**: All database operations are asynchronous
3. **Indexing**: Ensure your database tables have proper indexes
4. **Caching**: Consider implementing caching for frequently accessed data
5. **Pagination**: Use the `top` parameter for large result sets

## Troubleshooting

### Connection Issues
- Verify the connection string in `appsettings.json`
- Check if the SQL Server is running and accessible
- Verify firewall settings allow connections

### Authentication Errors
- Ensure SQL Server authentication is enabled
- Verify username and password are correct
- Check user permissions on the database

### Stored Procedure Not Found
- Verify the procedure exists in the database
- Check the schema name (default is `dbo`)
- Ensure the user has EXECUTE permissions

## Deployment

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["DBAccess.Api.csproj", "./"]
RUN dotnet restore "DBAccess.Api.csproj"
COPY . .
RUN dotnet build "DBAccess.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DBAccess.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DBAccess.Api.dll"]
```

### Azure App Service
1. Publish to Azure: `dotnet publish -c Release`
2. Deploy using Azure CLI or Visual Studio
3. Set connection string in Application Settings

### IIS
1. Publish the application: `dotnet publish -c Release`
2. Create an IIS application pool (.NET CLR version: No Managed Code)
3. Create a website pointing to the publish folder
4. Configure the connection string in `appsettings.json`
