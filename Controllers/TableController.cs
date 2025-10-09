using DBAccess.Api.Models;
using DBAccess.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBAccess.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TableController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<TableController> _logger;

    public TableController(IDatabaseService databaseService, ILogger<TableController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Query a database table
    /// </summary>
    /// <param name="request">The table query request</param>
    /// <returns>The query result</returns>
    [HttpPost("query")]
    public async Task<ActionResult<DatabaseExecutionResponse>> Query([FromBody] TableQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
        {
            return BadRequest(new DatabaseExecutionResponse 
            { 
                Success = false, 
                Message = "Table name is required" 
            });
        }

        _logger.LogInformation("Querying table: {TableName}", request.TableName);
        var result = await _databaseService.QueryTableAsync(request);
        
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
