using System.Text.Json;
using DBAccess.Api.Services;
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

    // POST /{dbName}/procedure/{procedureName}
    // Body: 任意 JSON（对象/数组/字符串/空），将作为单一 @inputValue 传入存储过程
    [HttpPost("{dbName}/procedure/{procedureName}")]
    public ActionResult<string> CallProcedure(
        [FromRoute] string dbName,
        [FromRoute] string procedureName,
        [FromBody] JsonElement? body)
    {
        string inputValue;
        if (body is null || body.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            inputValue = string.Empty;
        else if (body.Value.ValueKind == JsonValueKind.String)
            inputValue = body.Value.GetString() ?? string.Empty;
        else
            inputValue = body.Value.GetRawText(); // 原样 JSON 字符串

        var result = _db.Procedure(dbName, procedureName, new[] { "inputValue" }, new[] { inputValue });
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
        [FromQuery] string? offset,
        [FromBody] JsonElement? args)
    {
        string inputValue = string.Empty;

        if (args.HasValue)
        {
            var a = args.Value;
            switch (a.ValueKind)
            {
                case JsonValueKind.Array:
                    var parts = new List<string>();
                    foreach (var el in a.EnumerateArray())
                        parts.Add(ToSqlLiteral(el));
                    inputValue = string.Join(", ", parts); // 例: 'abc', 123, 1
                    break;
                case JsonValueKind.String:
                    inputValue = a.GetString() ?? string.Empty; // 允许传原始参数串
                    break;
                default:
                    // 其它形态作为单一 JSON 字符串实参
                    inputValue = ToSqlStringLiteral(a.GetRawText());
                    break;
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
