using DBAccess.Api.Models;

namespace DBAccess.Api.Services;

public interface IDatabaseService
{
    Task<DatabaseExecutionResponse> ExecuteStoredProcedureAsync(DatabaseExecutionRequest request);
    Task<DatabaseExecutionResponse> ExecuteFunctionAsync(DatabaseExecutionRequest request);
    Task<DatabaseExecutionResponse> QueryTableAsync(TableQueryRequest request);
}
