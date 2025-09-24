using System;
using System.Configuration;

namespace DBAccess
{
    internal static class ConnectionStrings
    {
        private const string SqlEnv = "DBACCESS_MSSQL";
        private const string MySqlEnv = "DBACCESS_MYSQL";

        public static string GetSqlServer()
        {
            var fromEnv = Environment.GetEnvironmentVariable(SqlEnv);
            if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv!;

            var cfg = ConfigurationManager.AppSettings["conStr"];
            if (!string.IsNullOrWhiteSpace(cfg)) return cfg!;

            return string.Empty;
        }

        public static string GetMySql()
        {
            var fromEnv = Environment.GetEnvironmentVariable(MySqlEnv);
            if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv!;

            var cs = ConfigurationManager.ConnectionStrings["mysql"]?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(cs)) return cs!;

            return string.Empty;
        }
    }
}
