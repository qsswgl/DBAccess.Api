using Dapper;
using DBAccess.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DBAccess.Api.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<DatabaseExecutionResponse> ExecuteStoredProcedureAsync(DatabaseExecutionRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = ConvertParameters(request.Parameters);
            
            var result = await connection.QueryAsync<dynamic>(
                request.Name,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return new DatabaseExecutionResponse
            {
                Success = true,
                Message = "Stored procedure executed successfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure: {ProcedureName}", request.Name);
            return new DatabaseExecutionResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<DatabaseExecutionResponse> ExecuteFunctionAsync(DatabaseExecutionRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build SQL to call function
            var parameterNames = request.Parameters?.Keys.ToList() ?? new List<string>();
            var parameterPlaceholders = string.Join(", ", parameterNames.Select(p => $"@{p}"));
            var sql = $"SELECT dbo.{request.Name}({parameterPlaceholders}) AS Result";

            var parameters = ConvertParameters(request.Parameters);
            
            var result = await connection.QueryAsync<dynamic>(sql, parameters);

            return new DatabaseExecutionResponse
            {
                Success = true,
                Message = "Function executed successfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function: {FunctionName}", request.Name);
            return new DatabaseExecutionResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<DatabaseExecutionResponse> QueryTableAsync(TableQueryRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build SQL query
            var sql = $"SELECT {(request.Top.HasValue ? $"TOP {request.Top.Value}" : "")} * FROM {request.TableName}";
            
            if (!string.IsNullOrWhiteSpace(request.WhereClause))
            {
                sql += $" WHERE {request.WhereClause}";
            }

            if (!string.IsNullOrWhiteSpace(request.OrderBy))
            {
                sql += $" ORDER BY {request.OrderBy}";
            }

            var parameters = ConvertParameters(request.Parameters);
            
            var result = await connection.QueryAsync<dynamic>(sql, parameters);

            return new DatabaseExecutionResponse
            {
                Success = true,
                Message = "Table query executed successfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying table: {TableName}", request.TableName);
            return new DatabaseExecutionResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private DynamicParameters? ConvertParameters(Dictionary<string, object?>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return null;

        var dynamicParams = new DynamicParameters();
        foreach (var param in parameters)
        {
            dynamicParams.Add(param.Key, param.Value);
        }
        return dynamicParams;
    }
}
