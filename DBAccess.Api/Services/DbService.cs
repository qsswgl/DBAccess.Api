using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Globalization;
using DBAccess.Api.Services.Security;

namespace DBAccess.Api.Services;

public class DbService
{
    private readonly string _baseConn;
    private readonly SqlGuardOptions _guard;

    // base connection string without Initial Catalog; DBName will be appended per request
    public DbService(string server, string user, string password, SqlGuardOptions? guard = null)
    {
        _baseConn = $"Data Source={server};User ID={user};Password={password};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=False;Connect Timeout=60";
        _guard = guard ?? new SqlGuardOptions();
    }

    private static string ToJsonArrayString(SqlDataReader reader)
    {
        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            var sb = new StringBuilder(s.Length + 16);
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (char.IsControl(ch))
                        {
                            sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:X4}", (int)ch);
                        }
                        else sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        var sbOut = new StringBuilder();
        sbOut.Append('[');
        bool firstRow = true;
        while (reader.Read())
        {
            if (!firstRow) sbOut.Append(',');
            firstRow = false;

            sbOut.Append('{');
            bool firstCol = true;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (!firstCol) sbOut.Append(',');
                firstCol = false;
                string key = reader.GetName(i);
                sbOut.Append('"').Append(Escape(key)).Append('"').Append(':');

                if (reader.IsDBNull(i))
                {
                    sbOut.Append("null");
                    continue;
                }

                var type = reader.GetFieldType(i);
                object val = reader.GetValue(i);
                if (type == typeof(string))
                {
                    sbOut.Append('"').Append(Escape(Convert.ToString(val) ?? string.Empty)).Append('"');
                }
                else if (type == typeof(bool))
                {
                    sbOut.Append(((bool)val) ? "true" : "false");
                }
                else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                {
                    sbOut.Append(Convert.ToString(val, CultureInfo.InvariantCulture));
                }
                else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                {
                    // ISO 8601
                    sbOut.Append('"').Append(Escape(Convert.ToDateTime(val, CultureInfo.InvariantCulture).ToString("O", CultureInfo.InvariantCulture))).Append('"');
                }
                else
                {
                    // 兜底：作为字符串输出
                    sbOut.Append('"').Append(Escape(Convert.ToString(val) ?? string.Empty)).Append('"');
                }
            }
            sbOut.Append('}');
        }
        reader.Close();
        sbOut.Append(']');
        return sbOut.ToString();
    }

    private static string GetTotalPage(SqlDataReader dataReader, string limit = "")
    {
        int total = 0;
        while (dataReader.Read())
        {
            var tStr = Convert.ToString(dataReader[0]) ?? "0";
            // 尽量安全解析为整数
            if (!int.TryParse(tStr, NumberStyles.Any, CultureInfo.InvariantCulture, out total))
            {
                // 回退：按十进制解析到 decimal 再取整数部分
                if (decimal.TryParse(tStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var td))
                {
                    total = (int)Math.Truncate(td);
                }
                else
                {
                    total = 0;
                }
            }
        }
        dataReader.Close();

        int page = 0;
        if (total > 0) page = 1;
        if (!string.IsNullOrEmpty(limit) && limit != "0" && total != 0)
        {
            if (int.TryParse(limit, NumberStyles.Any, CultureInfo.InvariantCulture, out var lim) && lim > 0)
            {
                page = (int)Math.Ceiling(total / (double)lim);
            }
        }

        // 返回数值型的 JSON 片段（不带引号）
        return $"\"total\":{total},\"page\":{page}";
    }

    public string Procedure(string DBName, string ProcedureName, string[] InputName, string[] InputValue)
    {
        var conStr = _baseConn + $";Initial Catalog={DBName}";
        using var con = new SqlConnection(conStr);
        try
        {
            using var cmd = new SqlCommand(ProcedureName, con);
            cmd.CommandType = CommandType.StoredProcedure;
            string Parame_InputValue = "";
            int i = 0;
            foreach (string s in InputName)
            {
                Parame_InputValue = InputValue[i];
                cmd.Parameters.AddWithValue("@" + s, Parame_InputValue);
                i = i + 1;
            }
            var parOutput = cmd.Parameters.Add("@OutputValue", SqlDbType.VarChar, 102400000);
            parOutput.Direction = ParameterDirection.Output;
            var parReturn = new SqlParameter("@return", SqlDbType.Int, 10);
            parReturn.Direction = ParameterDirection.ReturnValue;
            cmd.Parameters.Add(parReturn);
            cmd.CommandTimeout = 6000;
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
            var ReturnJson = Convert.ToString(parOutput.Value) ?? string.Empty;
            return ReturnJson;
        }
        catch (Exception ex)
        {
            return $"{{\"Result\":\"-1\",\"Message\":\"{ex.Message}\"}}";
        }
    }

    public string Function(string DBName, string FunctionName, string InputValue, string Fields = "*", string WhereStr = "", string OrderStr = "", string Limit = "", string Offset = "0")
    {
        var conStr = _baseConn + $";Initial Catalog={DBName}";
        using var con = new SqlConnection(conStr);
        try
        {
            SqlValidator.ValidateFunction(FunctionName, _guard);
            SqlValidator.ValidateColumns(Fields, _guard);
            string sanitizedWhere;
            if (_guard.UseStrictWhereParser)
            {
                // 严格模式：AST 解析
                sanitizedWhere = SqlWhereParser.ParseAndSanitize(WhereStr, _guard, out _);
            }
            else
            {
                // 宽松模式：仅字符白名单 + 危险关键字拦截
                sanitizedWhere = WhereStr ?? string.Empty;
                SqlValidator.ValidateWhereOrOrder(sanitizedWhere, _guard);
            }
            SqlValidator.ValidateWhereOrOrder(OrderStr, _guard);

            // clamp limit/offset
            var lim = SqlValidator.ValidateAndClampLimit(Limit, _guard);
            var off = SqlValidator.ValidateAndClampOffset(Offset, _guard);

            if (!string.IsNullOrEmpty(sanitizedWhere)) WhereStr = " Where " + sanitizedWhere; else WhereStr = string.Empty;
            if (!string.IsNullOrEmpty(OrderStr)) OrderStr = " Order By " + OrderStr;
            string PageStr = "";
            if (!string.IsNullOrEmpty(OrderStr) && lim > 0)
            {
                PageStr = $" offset {off} rows fetch next {lim} rows only ";
            }
            // 规范化 dbo 前缀
            if (!FunctionName.Contains(".dbo.", StringComparison.OrdinalIgnoreCase)) FunctionName = " Dbo." + FunctionName;
            string SelectStr = $"Select {Fields} From {FunctionName}({InputValue}){WhereStr}{OrderStr}{PageStr}";
            using var cmd = new SqlCommand(SelectStr, con) { CommandType = CommandType.Text, CommandTimeout = 600 };
            con.Open();
            using var DR = cmd.ExecuteReader();
            var ReturnJson = ToJsonArrayString(DR);
            string TotalPage = ""; string TotalSelectStr = $"Select  Count(*) As total From {FunctionName}({InputValue}){WhereStr}";
            cmd.CommandText = TotalSelectStr;
            using var DR2 = cmd.ExecuteReader();
            TotalPage = GetTotalPage(DR2, lim.ToString(CultureInfo.InvariantCulture));
            con.Close();
            ReturnJson = "{" + TotalPage + ",\"rows\":" + ReturnJson + "}";
            return ReturnJson;
        }
        catch (Exception ex)
        {
            return $"{{\"Result\":\"-1\",\"Message\":\"{ex.Message}\"}}";
        }
    }

    public string Table(string DBName, string TableName, string WhereStr = "", string Fields = "*", string OrderStr = "", string Limit = "", string Offset = "0")
    {
        var conStr = _baseConn + $";Initial Catalog={DBName}";
        using var con = new SqlConnection(conStr);
        try
        {
            SqlValidator.ValidateTable(TableName, _guard);
            SqlValidator.ValidateColumns(Fields, _guard);
            string sanitizedWhere;
            if (_guard.UseStrictWhereParser)
            {
                sanitizedWhere = SqlWhereParser.ParseAndSanitize(WhereStr, _guard, out _);
            }
            else
            {
                sanitizedWhere = WhereStr ?? string.Empty;
                SqlValidator.ValidateWhereOrOrder(sanitizedWhere, _guard);
            }
            SqlValidator.ValidateWhereOrOrder(OrderStr, _guard);

            var lim = SqlValidator.ValidateAndClampLimit(Limit, _guard);
            var off = SqlValidator.ValidateAndClampOffset(Offset, _guard);

            if (!string.IsNullOrEmpty(sanitizedWhere)) WhereStr = " Where " + sanitizedWhere; else WhereStr = string.Empty;
            if (!string.IsNullOrEmpty(OrderStr)) OrderStr = " Order By " + OrderStr;
            string PageStr = "";
            if (!string.IsNullOrEmpty(OrderStr) && lim > 0)
            {
                PageStr = $" offset {off} rows fetch next {lim} rows only ";
            }
            string SelectStr = $"Select {Fields} From {TableName}{WhereStr}{OrderStr}{PageStr}";
            using var cmd = new SqlCommand(SelectStr, con) { CommandType = CommandType.Text };
            con.Open();
            using var DR = cmd.ExecuteReader();
            var ReturnJson = ToJsonArrayString(DR);
            string TotalPage = ""; string TotalSelectStr = $"Select  Count(*) As total From {TableName}{WhereStr}";
            cmd.CommandText = TotalSelectStr;
            using var DR2 = cmd.ExecuteReader();
            TotalPage = GetTotalPage(DR2, lim.ToString(CultureInfo.InvariantCulture));
            con.Close();
            ReturnJson = "{" + TotalPage + ",\"rows\":" + ReturnJson + "}";
            return ReturnJson;
        }
        catch (Exception ex)
        {
            return $"{{\"Result\":\"-1\",\"Message\":\"{ex.Message}\"}}";
        }
    }
}
