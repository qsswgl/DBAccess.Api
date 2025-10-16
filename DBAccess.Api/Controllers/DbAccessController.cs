using System.Text.Json;
using DBAccess.Api.Services;
using DBAccess.Api.Models;
using DBAccess.Api.Json;
using Microsoft.AspNetCore.Mvc;

namespace DBAccess.Api.Controllers;

[ApiController]
[Route("")]
[ApiExplorerSettings(GroupName = "v1")]
[Produces("application/json")]
public class RestDbController : ControllerBase
{
    private readonly DbService _db;
    public RestDbController(DbService db) => _db = db;

    /// <summary>
    /// 调用存储过程
    /// </summary>
    /// <param name="dbName">数据库名称</param>
    /// <param name="procedureName">存储过程名称</param>
    /// <param name="input">JSON格式的输入参数</param>
    /// <returns>存储过程执行结果</returns>
    /// <remarks>
    /// 输入参数将被序列化为JSON字符串，作为@inputValue传递给存储过程
    /// 
    /// 示例：调用 DNSPOD_UPDATE 存储过程
    /// </remarks>
    [HttpPost("{dbName}/procedure/{procedureName}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(string))]
    [ProducesResponseType(500)]
    public async Task<ActionResult<string>> CallProcedure(
        [FromRoute] string dbName,
        [FromRoute] string procedureName,
        [FromBody] ProcedureInputModel input)
    {
        string inputString = string.Empty;
        
        if (input != null)
        {
            // 将输入对象序列化为JSON字符串，使用AOT兼容的上下文
            inputString = JsonSerializer.Serialize(input, AppJsonContext.Default.ProcedureInputModel);
        }

        var result = _db.Procedure(dbName, procedureName, new[] { "inputValue" }, new[] { inputString });
        return Content(result, "application/json");
    }

    // GET /{dbName}/table/{tableName}?fields=*&where=...&order=...&limit=...&offset=0
    [HttpGet("{dbName}/table/{tableName}")]
    public ActionResult<string> QueryTable(
        [FromRoute] string dbName,
        [FromRoute] string tableName,
        [FromQuery(Name = "where")] string? whereStr,
        [FromQuery(Name = "fields")] string? fields,
        [FromQuery(Name = "order")] string? orderStr,
        [FromQuery] string? limit,
        [FromQuery] string? offset)
    {
        var result = _db.Table(dbName, tableName,
            whereStr ?? string.Empty, fields ?? "*", orderStr ?? string.Empty,
            limit ?? string.Empty, offset ?? "0");
        return Content(result, "application/json");
    }

    // POST /{dbName}/function/{functionName}?fields=*&where=...&order=...&limit=...&offset=0
    // Body: 参数数组（如 ["abc", 123, true]）或字符串（原样传递）或对象（作为单一 JSON 字符串参数）
    [HttpPost("{dbName}/function/{functionName}")]
    public ActionResult<string> CallFunction(
        [FromRoute] string dbName,
        [FromRoute] string functionName,
        [FromQuery(Name = "where")] string? whereStr,
        [FromQuery(Name = "fields")] string? fields,
        [FromQuery(Name = "order")] string? orderStr,
        [FromQuery] string? limit,
        [FromQuery] string? offset)
    {
        string inputValue = string.Empty;

        if (Request.Body != null && Request.ContentLength > 0)
        {
            using var reader = new StreamReader(Request.Body);
            var bodyContent = reader.ReadToEnd();
            
            if (!string.IsNullOrEmpty(bodyContent))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(bodyContent);
                    var jsonElement = jsonDoc.RootElement;
                    
                    switch (jsonElement.ValueKind)
                    {
                        case JsonValueKind.Array:
                            var parts = new List<string>();
                            foreach (var el in jsonElement.EnumerateArray())
                                parts.Add(ToSqlLiteral(el));
                            inputValue = string.Join(", ", parts);
                            break;
                        case JsonValueKind.String:
                            inputValue = jsonElement.GetString() ?? string.Empty;
                            break;
                        default:
                            inputValue = ToSqlStringLiteral(jsonElement.GetRawText());
                            break;
                    }
                }
                catch
                {
                    // 如果不是有效JSON，直接使用原始字符串
                    inputValue = bodyContent;
                }
            }
        }

        var result = _db.Function(dbName, functionName,
            inputValue, fields ?? "*", whereStr ?? string.Empty, orderStr ?? string.Empty,
            limit ?? string.Empty, offset ?? "0");
        return Content(result, "application/json");
    }

    private static string ToSqlLiteral(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => ToSqlStringLiteral(el.GetString() ?? string.Empty),
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "1",
        JsonValueKind.False => "0",
        JsonValueKind.Null => "NULL",
        _ => ToSqlStringLiteral(el.GetRawText())
    };

    private static string ToSqlStringLiteral(string s)
        => "'" + s.Replace("'", "''") + "'";
}
