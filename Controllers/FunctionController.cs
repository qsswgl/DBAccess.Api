using DBAccess.Api.Models;
using DBAccess.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBAccess.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FunctionController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<FunctionController> _logger;

    public FunctionController(IDatabaseService databaseService, ILogger<FunctionController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Execute a database function
    /// </summary>
    /// <param name="request">The function name and parameters</param>
    /// <returns>The execution result</returns>
    [HttpPost("execute")]
    public async Task<ActionResult<DatabaseExecutionResponse>> Execute([FromBody] DatabaseExecutionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new DatabaseExecutionResponse 
            { 
                Success = false, 
                Message = "Function name is required" 
            });
        }

        _logger.LogInformation("Executing function: {FunctionName}", request.Name);
        var result = await _databaseService.ExecuteFunctionAsync(request);
        
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
