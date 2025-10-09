using DBAccess.Api.Models;
using DBAccess.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBAccess.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoredProcedureController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<StoredProcedureController> _logger;

    public StoredProcedureController(IDatabaseService databaseService, ILogger<StoredProcedureController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Execute a stored procedure
    /// </summary>
    /// <param name="request">The stored procedure name and parameters</param>
    /// <returns>The execution result</returns>
    [HttpPost("execute")]
    public async Task<ActionResult<DatabaseExecutionResponse>> Execute([FromBody] DatabaseExecutionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new DatabaseExecutionResponse 
            { 
                Success = false, 
                Message = "Stored procedure name is required" 
            });
        }

        _logger.LogInformation("Executing stored procedure: {ProcedureName}", request.Name);
        var result = await _databaseService.ExecuteStoredProcedureAsync(request);
        
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
