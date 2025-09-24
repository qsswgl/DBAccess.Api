using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using MySql.Data.MySqlClient;

namespace DBAccess
{
  public class Interface
  {

    string ReturnJson = "";
    string conStr = ConnectionStrings.GetSqlServer();
        //string conStr = "Data Source =localhost; user id = sa; pwd = qsswgl_5988856";
        //string conStr = "Data Source = 192.168.1.130; user id = sa; pwd = qsswgl_5988856";

        private string MysqlProcedure(string DBName, string ProcedureName, string[] InputName, string[] InputValue)
        {
            conStr = ConnectionStrings.GetMySql();
            conStr += ";Database=" + DBName;
            MySqlConnection con = new MySqlConnection(conStr);
            MySqlCommand cmd = new MySqlCommand(ProcedureName, con);
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
//                cmd.Connection.Close();
  //              cmd.Dispose();
                string test = ex.StackTrace ?? string.Empty;
                return "{\"Result\":\"" + "-1" + "\",\"Message\":\"" + ex.Message + "\"}";
            }
            finally
            {
                cmd.Connection.Close();
                cmd.Dispose();
            }
        }

        public string Procedure(string DBName,string ProcedureName, string[] InputName, string[] InputValue)
        {
            //创建数据库链接
            //创建SqlConnection对象用以传递数据库链接字符串。
            conStr += "; initial catalog = " + DBName;
            using (SqlConnection con = new SqlConnection(conStr)) //打开数据库链接
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(ProcedureName, con); //创建sqlCommand对象,api 为存储过程名称
                    cmd.CommandType = CommandType.StoredProcedure;
                    string Parame_InputValue = "";
                    int i = 0;
                    foreach (string s in InputName)
                    {
                        Parame_InputValue = InputValue[i];
                        cmd.Parameters.AddWithValue("@" + @s, Parame_InputValue);  //给输入参数赋值  
                        i = i + 1;
                    }
                    SqlParameter parOutput = cmd.Parameters.Add("@OutputValue", SqlDbType.VarChar, 102400000);//定义输出参数 
                    parOutput.Direction = ParameterDirection.Output;  //参数类型为Output  
                    SqlParameter parReturn = new SqlParameter("@return", SqlDbType.Int, 10); //定义返回值
                    parReturn.Direction = ParameterDirection.ReturnValue;   //参数类型为ReturnValue  
                    cmd.Parameters.Add(parReturn); //添加存储过程返回值
                    cmd.CommandTimeout = 6000; //'设置数据库执行超时时间为600秒
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                        //在数据库链接断掉之后才可以获取到值。
                        ReturnJson = Convert.ToString(parOutput.Value) ?? string.Empty;//获取存储过程输出参数的值
                    int ReturnValue = Convert.ToInt16(parReturn.Value);
                    return ReturnJson;

                }

                catch (Exception ex)
                {
                    return "{\"Result\":\"" + "-1" + "\",\"Message\":\"" + ex.Message + "\"}";
                }
            }

        }

        public string Function(string DBName, string FunctionName,string InputValue,  string Fields="*",string WhereStr ="", string OrderStr="",string Limit ="",string Offset="0")
        {
            conStr += ";Initial Catalog=" + DBName;
            using (SqlConnection con = new SqlConnection(conStr)) //打开数据库链接
            {
                try
                {
                    if (WhereStr != "")
                    {
                        WhereStr = " Where " + WhereStr;
                    }
                    if (OrderStr!="") {
                        OrderStr = " Order By " + OrderStr;
                    }
                    string PageStr = "";
                    if (OrderStr !="" && Limit != "")
                    {
                        PageStr = " offset " + Offset + " rows fetch next " + Limit + " rows only ";
                    }
                    if (FunctionName.ToLower().Contains(".dbo.") == false)
                    {
                        FunctionName = " Dbo." + FunctionName;
                    }
                    string SelectStr = "Select " + Fields + " From " + FunctionName + "(" + InputValue + ")" + WhereStr + OrderStr + PageStr;
                    //SelectStr = "Select 'a\"b' as Content ";
                    SqlCommand cmd = new(); //创建sqlCommand对象,api 为存储过程名称
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.Text;
                    SqlDataAdapter SqlAda=new();
                    cmd.CommandText = SelectStr;
                    cmd.CommandTimeout = 600; //'设置数据库执行超时时间为600秒
                    SqlAda.SelectCommand = cmd;
                    con.Open();
                    SqlDataReader DR = cmd.ExecuteReader();
                    ReturnJson = ToJsonArrayString(DR);
                    String TotalPage=""; string TotalSelectStr = "";
                    TotalSelectStr = "Select  Count(*) As total From " + FunctionName + "(" + InputValue + ")" + WhereStr;
                    cmd.CommandText = TotalSelectStr;
                    DR = cmd.ExecuteReader();
                    TotalPage=GetTotalPage(DR, Limit);
                    con.Close();
                    ReturnJson = "{" + TotalPage + ",\"rows\":" + ReturnJson + "}";
                    return ReturnJson;

                }

                catch (Exception ex)
                {
                    return "{\"Result\":\"" + "-1" + "\",\"Message\":\"" + ex.Message + "\"}";
                }
            }

        }

        public string Table(string DBName, string TableName, string WhereStr = "", string Fields = "*",  string OrderStr = "", string Limit = "", string Offset = "0")
        {
            conStr += ";Initial Catalog=" + DBName;
            using (SqlConnection con = new SqlConnection(conStr)) //打开数据库链接
            {
                try
                {
                    if (WhereStr != "")
                    {
                        WhereStr = " Where " + WhereStr;
                    }
                    if (OrderStr != "")
                    {
                        OrderStr = " Order By " + OrderStr;
                    }
                    string PageStr = "";
                    if (OrderStr != "" && Limit != "")
                    {
                        PageStr = " offset "+Offset+" rows fetch next "+Limit+" rows only ";
                    }
                    string SelectStr = "Select " + Fields + " From " + TableName + WhereStr +OrderStr+ PageStr;
                    SqlCommand cmd = new(); //创建sqlCommand对象,api 为存储过程名称
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.Text;
                    SqlDataAdapter SqlAda = new();
                    cmd.CommandText = SelectStr;
                    //Com.CommandTimeout = 180 '设置数据库查询超时时间为180秒
                    SqlAda.SelectCommand = cmd;
                    con.Open();
                    SqlDataReader DR = cmd.ExecuteReader();
                    ReturnJson = ToJsonArrayString(DR);
                    String TotalPage = ""; string TotalSelectStr = "";
                    TotalSelectStr = "Select  Count(*) As total From " + TableName +  WhereStr;
                    cmd.CommandText = TotalSelectStr;
                    DR = cmd.ExecuteReader();
                    TotalPage = GetTotalPage(DR, Limit);
                    con.Close();
                    ReturnJson = "{" + TotalPage + ",\"rows\":" + ReturnJson + "}";
                    return ReturnJson;
                }

                catch (Exception ex)
                {
                    return "{\"Result\":\"" + "-1" + "\",\"Message\":\"" + ex.Message + "\"}";
                }
            }

        }

        /// <summary>   
        /// DataReader转换为Json   
        /// </summary>   
        /// <param name="dataReader">DataReader对象</param>   
        /// <returns>Json字符串(数组）</returns>   
        public static string ToJsonArrayString(SqlDataReader dataReader)
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("[");
            while (dataReader.Read())
            {
                jsonString.Append("{");
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    Type type = dataReader.GetFieldType(i);
                    string strKey = dataReader.GetName(i);
                    object? rawVal = dataReader.IsDBNull(i) ? null : dataReader.GetValue(i);
                    string strValue = rawVal?.ToString() ?? string.Empty;
                    jsonString.Append("\"" + strKey + "\":");
                    // 非字符串类型保持原值；已在上方进行空安全处理
                    //datetime和int类型不能出现为空的情况,所以将其转换成字符串来进行处理。
                    //需要加""的
                    if (type == typeof(string) || type == typeof(DateTime) || type == typeof(int))
                    {
                        if (i <= dataReader.FieldCount - 1)
                        {

                            jsonString.Append("\"" + strValue.Replace("\"","\\\"") + "\",");
                            //jsonString.Append("\"" + strValue+ "\",");
                        }
                        else
                        {
                            jsonString.Append(strValue);
                        }
                    }
                    //不需要加""的
                    else
                    {
                        if (i <= dataReader.FieldCount - 1)
                        {
                            jsonString.Append("" + strValue + ",");
                        }
                        else
                        {
                            jsonString.Append(strValue);
                        }
                    }
                }

                jsonString.Append("},");
            }
            dataReader.Close();
            //当读取到的数据为空，此时jsonString中只有一个字符"["
            if (jsonString.Length == 1)
            {
                jsonString.Append("]");
            }
            else//数据不为空
            {
                //所有数据读取完成，移除最后三个多余的",},"字符
                jsonString.Remove(jsonString.Length - 3, 3);
                jsonString.Append("}");
                jsonString.Append("]");
            }

            return jsonString.ToString();
        }

        public static string GetTotalPage(SqlDataReader dataReader,string Limit="")
        {
            string jsonString="";
            while (dataReader.Read())
            {
                    string Total = Convert.ToString(dataReader[0]) ?? "0";
                    jsonString=jsonString+"\"" + "total" + "\":";
                    jsonString = jsonString + "\"" + Total + "\",";
                    jsonString = jsonString +"\"" + "page" + "\":";
                    decimal Page=0;
                   if (Convert.ToDecimal(Total) > 0)
                   {
                     Page = 1;
                   }
                    if (Limit != "" && Limit !="0" && Total !="0" )
                    {
                        Page = Convert.ToDecimal(Total) / Convert.ToDecimal(Limit);
                        Page = Math.Ceiling(Page);
                    }
                    jsonString = jsonString + "\"" + Page + "\"";
            }
            dataReader.Close();
            return jsonString;
        }
        /// <summary>
        /// 获取配置项
        /// </summary>
        /// <param name="key">配置文件中key字符串</param>
        /// <returns></returns>
        public static string GetappSettings(string key)
        {
            try
            {
                //打开配置文件 
                return "Data Source =127.0.0.1; user id = sa; pwd = qsswgl_5988856";


            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
