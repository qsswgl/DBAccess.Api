namespace DBAccess.Api.Services.Security;

public class SqlGuardOptions
{
    public bool EnableWhitelist { get; set; } = false;
    // 是否启用严格的 WHERE 表达式解析器（AST）。默认关闭，走宽松校验。
    public bool UseStrictWhereParser { get; set; } = false;
    public HashSet<string> AllowedTables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedFunctions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedColumns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> BlockedTokens { get; set; } = new()
    {
        "--", ";", "/*", "*/", " xp_", ";exec", " union ", " drop ", " insert ", " update ", " delete ", " truncate ", " alter ", " create ", ";--",
        // 一些常见危险关键字/函数/系统对象
        " exec ", " execute ", " grant ", " revoke ", " backup ", " restore ", " or 1=1", " or '1'='1", " information_schema.", " sys.", " openrowset", " openquery"
    };

    // 分页限制，避免过大扫描造成压力
    public int MaxPageSize { get; set; } = 1000;
    public int MaxOffset { get; set; } = 1_000_000;
}
