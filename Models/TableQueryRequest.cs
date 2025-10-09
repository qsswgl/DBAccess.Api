namespace DBAccess.Api.Models;

public class TableQueryRequest
{
    public string TableName { get; set; } = string.Empty;
    public string? WhereClause { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public int? Top { get; set; }
    public string? OrderBy { get; set; }
}
