using MySql.Data.MySqlClient;
using System.Data;
using System.Text;
namespace DBAccess
{
    public class Mysqlinterface
    {
    string ReturnJson = "";
    string conStr = ConnectionStrings.GetMySql(); 
        public  string Procedure(string DBName, string ProcedureName, string[] InputName, string[] InputValue)
        {
            conStr += ";Database=" + DBName;
            MySqlConnection con = new MySqlConnection(conStr);
            MySqlCommand cmd = new MySqlCommand(ProcedureName,con);
//            cmd.Connection = con;
            cmd.CommandTimeout = 600;
            try
            {
                cmd.Connection.Open();
//                cmd.CommandType = CommandType.StoredProcedure;
  //              cmd.CommandText = ProcedureName;
                string Parame_InputValue = "";
                int i = 0;
                foreach (string s in InputName)
                {
                    Parame_InputValue = InputValue[i];
                    MySqlParameter parameter;
                    parameter = new MySqlParameter("?" + @s, MySqlDbType.String);
                    parameter.Value = Parame_InputValue;
                    parameter.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(parameter);
                    i += 1;
                }
                MySqlParameter parOutput;
                parOutput = new MySqlParameter("?OutPutValue", MySqlDbType.String);
                parOutput.Direction = ParameterDirection.Output;
                parOutput.Value = "调用存储过程后返回的值，等待调用返回中";
                cmd.Parameters.Add(parOutput);  //定义输出参数
                cmd.ExecuteNonQuery();
                ReturnJson = parOutput.Value?.ToString() ?? string.Empty;//获取存储过程输出参数的值
                return ReturnJson;
            }
            catch (MySqlException ex)
            {
                cmd.Connection.Close();
                cmd.Dispose();
                return "{\"Result\":\"" + "-1" + "\",\"Message\":\"" + ex.Message + "\"}";
            }
            finally
            {
                cmd.Connection.Close();
                cmd.Dispose();
            }
        }
    }
}