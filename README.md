# DBAccess.Api
.NET 9 ASP.NET Core Web API project for accessing database stored procedures, functions, and tables.

## Features

This API provides endpoints to:
- Execute stored procedures
- Execute database functions
- Query database tables

## Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourDatabase;User Id=YourUsername;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

## API Endpoints

### 1. Execute Stored Procedure
**POST** `/api/storedprocedure/execute`

Execute a SQL Server stored procedure with optional parameters.

**Request Body:**
```json
{
  "name": "sp_GetUsers",
  "parameters": {
    "UserId": 1,
    "Status": "Active"
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Stored procedure executed successfully",
  "data": [
    {
      "id": 1,
      "name": "John Doe",
      "status": "Active"
    }
  ],
  "rowsAffected": null
}
```

### 2. Execute Database Function
**POST** `/api/function/execute`

Execute a SQL Server scalar or table-valued function.

**Request Body:**
```json
{
  "name": "fn_CalculateTotal",
  "parameters": {
    "Amount": 100,
    "TaxRate": 0.15
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Function executed successfully",
  "data": [
    {
      "result": 115
    }
  ],
  "rowsAffected": null
}
```

### 3. Query Table
**POST** `/api/table/query`

Query a database table with optional filtering, ordering, and limiting.

**Request Body:**
```json
{
  "tableName": "Users",
  "whereClause": "Status = @Status AND CreatedDate > @Date",
  "parameters": {
    "Status": "Active",
    "Date": "2024-01-01"
  },
  "top": 10,
  "orderBy": "CreatedDate DESC"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Table query executed successfully",
  "data": [
    {
      "id": 1,
      "name": "John Doe",
      "status": "Active",
      "createdDate": "2024-01-15"
    }
  ],
  "rowsAffected": null
}
```

## Running the API

1. Clone the repository
2. Update the connection string in `appsettings.json`
3. Run the application:
   ```bash
   dotnet run
   ```
4. Navigate to `https://localhost:5001/swagger` to view the API documentation

## Technologies

- .NET 9
- ASP.NET Core Web API
- Dapper (Micro ORM)
- Microsoft.Data.SqlClient
- Swagger/OpenAPI

## Security Considerations

- **SQL Injection Protection**: All queries use parameterized inputs via Dapper
- **Connection String**: Store sensitive connection strings in environment variables or Azure Key Vault in production
- **Input Validation**: Validate table names and procedure names to prevent unauthorized access
- **Authentication**: Consider adding authentication and authorization middleware for production use

## License

This project is provided as-is for database access operations.
