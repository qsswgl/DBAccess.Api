using System.Text.RegularExpressions;
using System.Linq;

namespace DBAccess.Api.Services.Security;

public static class SqlValidator
{
    private static readonly Regex IdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_\.]*$");
    private static readonly Regex ColumnsRegex = new(@"^[A-Za-z0-9_, \[\]\*\.]+$");
    // 单列名：允许 [name] 或 name，允许最多两级限定（例如 t.[col] 或 dbo.t.col），不允许空格/别名/函数
    private static readonly Regex ColumnTokenRegex = new(@"^\s*(\*|\[?[A-Za-z_][A-Za-z0-9_]*\]?(\.\[?[A-Za-z_][A-Za-z0-9_]*\]?){0,2})\s*$");
    // 允许的 WHERE 片段字符：字母数字、空格、点、下划线、括号、比较/逻辑/算术运算符、引号、百分号、逗号
    private static readonly Regex WhereAllowedRegex = new(@"^[A-Za-z0-9_\.\s\(\)<>!=%\+\-\*/'"",]+$");
    // 允许的 ORDER BY：列名与逗号、空格、ASC/DESC、点、方括号
    private static readonly Regex OrderAllowedRegex = new(@"^[A-Za-z0-9_\s\.,\[\]]+(ASC|DESC|asc|desc)?[A-Za-z0-9_\s\.,\[\]]*$");

    public static void ValidateTable(string table, SqlGuardOptions opt)
    {
        if (!IdentifierRegex.IsMatch(table)) throw new ArgumentException("非法表名");
        if (opt.EnableWhitelist && opt.Alowed(table: table, type: "table") == false)
            throw new ArgumentException("表不在白名单");
        CheckBlocked(table, opt);
    }

    public static void ValidateFunction(string func, SqlGuardOptions opt)
    {
        if (!IdentifierRegex.IsMatch(func)) throw new ArgumentException("非法函数名");
        if (opt.EnableWhitelist && opt.Alowed(table: func, type: "function") == false)
            throw new ArgumentException("函数不在白名单");
        CheckBlocked(func, opt);
    }

    public static void ValidateColumns(string columns, SqlGuardOptions opt)
    {
        // 基础字符集校验
        if (!ColumnsRegex.IsMatch(columns)) throw new ArgumentException("非法列名列表");
        // 逐列严格校验
        var rawCols = columns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var rc in rawCols)
        {
            if (!ColumnTokenRegex.IsMatch(rc))
                throw new ArgumentException($"列名不被允许: {rc}");
        }
        if (opt.EnableWhitelist && columns.Trim() != "*")
        {
            foreach (var rc in rawCols)
            {
                if (rc.Trim() == "*") continue;
                // 提取最后一级名称作为列名进行白名单比对
                var t = rc.Trim();
                var parts = t.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var last = parts.Last().Trim().Trim('[', ']');
                if (!opt.AllowedColumns.Contains(last))
                    throw new ArgumentException($"列不在白名单: {last}");
            }
        }
        CheckBlocked(columns, opt);
    }

    public static void ValidateWhereOrOrder(string fragment, SqlGuardOptions opt)
    {
        if (string.IsNullOrWhiteSpace(fragment)) return;
        var text = fragment.Trim();
        // 判定是否像 ORDER BY 还是 WHERE 片段，简单依据含有逗号和 ASC/DESC 出现位置
        bool likeOrder = Regex.IsMatch(text, @"\b(ASC|DESC)\b", RegexOptions.IgnoreCase) || text.Contains(',');
        if (likeOrder)
        {
            if (!OrderAllowedRegex.IsMatch(text)) throw new ArgumentException("非法的 ORDER 片段");
        }
        else
        {
            if (!WhereAllowedRegex.IsMatch(text)) throw new ArgumentException("非法的 WHERE 片段");
        }
        // 依然做危险关键字拦截
        CheckBlocked(text, opt);
    }

    public static int ValidateAndClampLimit(string limit, SqlGuardOptions opt)
    {
        if (string.IsNullOrWhiteSpace(limit)) return 0;
        if (!int.TryParse(limit, out var lim) || lim < 0) throw new ArgumentException("Limit 非法");
        if (lim == 0) return 0;
        return Math.Min(lim, opt.MaxPageSize);
    }

    public static int ValidateAndClampOffset(string offset, SqlGuardOptions opt)
    {
        if (string.IsNullOrWhiteSpace(offset)) return 0;
        if (!int.TryParse(offset, out var off) || off < 0) throw new ArgumentException("Offset 非法");
        return Math.Min(off, opt.MaxOffset);
    }

    private static void CheckBlocked(string text, SqlGuardOptions opt)
    {
        var lower = text.ToLowerInvariant();
        foreach (var bad in opt.BlockedTokens)
        {
            if (lower.Contains(bad))
                throw new ArgumentException("包含非法关键词");
        }
    }

    private static bool Alowed(this SqlGuardOptions opt, string table, string type)
    {
        return type == "table" ? opt.AllowedTables.Contains(table) : opt.AllowedFunctions.Contains(table);
    }
}
