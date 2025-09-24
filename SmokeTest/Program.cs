using System;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;

internal class Program
{
	private static int TestSqlServer()
	{
		var cs = Environment.GetEnvironmentVariable("DBACCESS_MSSQL");
		if (string.IsNullOrWhiteSpace(cs))
		{
			Console.WriteLine("[MSSQL] 跳过：未设置环境变量 DBACCESS_MSSQL");
			return 0;
		}
		try
		{
			using var conn = new SqlConnection(cs);
			conn.Open();
			using var cmd = new SqlCommand("SELECT 1", conn);
			var r = Convert.ToInt32(cmd.ExecuteScalar());
			Console.WriteLine($"[MSSQL] OK - SELECT 1 => {r}");
			return 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[MSSQL] 失败: {ex.Message}");
			return 1;
		}
	}

	private static int TestMySql()
	{
		var cs = Environment.GetEnvironmentVariable("DBACCESS_MYSQL");
		if (string.IsNullOrWhiteSpace(cs))
		{
			Console.WriteLine("[MySQL] 跳过：未设置环境变量 DBACCESS_MYSQL");
			return 0;
		}
		try
		{
			using var conn = new MySqlConnection(cs);
			conn.Open();
			using var cmd = new MySqlCommand("SELECT 1", conn);
			var r = Convert.ToInt32(cmd.ExecuteScalar());
			Console.WriteLine($"[MySQL] OK - SELECT 1 => {r}");
			return 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[MySQL] 失败: {ex.Message}");
			return 1;
		}
	}

	public static int Main(string[] args)
	{
		var rc1 = TestSqlServer();
		var rc2 = TestMySql();
		return (rc1 != 0 || rc2 != 0) ? 1 : 0;
	}
}
