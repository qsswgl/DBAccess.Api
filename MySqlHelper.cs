using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAccess
{
    internal class MySqlHelper
    {
    public static string ConnStr = ConnectionStrings.GetMySql();


        //打开数据库链接
    public static MySqlConnection? Open_Conn(string ConnStr)
        {
            try
            {
                MySqlConnection Conn = new MySqlConnection(ConnStr + "Connect Timeout=60000;");
                Conn.Open();
                return Conn;
            }
            catch
            {
                return null;
                //XtraMessageBox.Show("新的工程建立成功！","", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //关闭数据库链接
        public static void Close_Conn(MySqlConnection? Conn)
        {
            if (Conn != null)
            {
                Conn.Close();
                Conn.Dispose();
            }
        }

        //运行MySql语句
        public static int Run_SQL(string SQL)
        {
            try
            {
                MySqlConnection? Conn = Open_Conn(ConnStr);
                if (Conn != null)
                {
                    MySqlCommand Cmd = Create_Cmd(SQL, Conn);

                    try
                    {
                        int result_count = Cmd.ExecuteNonQuery();
                        Close_Conn(Conn);
                        return 1;
                    }
                    catch
                    {
                        Close_Conn(Conn);

                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        // 生成Command对象 
        public static MySqlCommand Create_Cmd(string SQL, MySqlConnection Conn)
        {
            MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
            return Cmd;
        }

        // 运行MySql语句返回 DataTable
        public static DataTable? Get_DataTable(string SQL)
        {
            try
            {
                MySqlConnection? Conn = Open_Conn(ConnStr);
                if (Conn != null)
                {
                    MySqlDataAdapter Da = new MySqlDataAdapter(SQL, Conn);
                    DataTable dt = new DataTable();
                    Da.Fill(dt);
                    Close_Conn(Conn);
                    return dt;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

        }

        public static DataTable? Get_DataTable(string SQL1, string SQL2, string Table1, string Table2, string Relation, string Discrible)
        {
            MySqlConnection? Conn = Open_Conn(ConnStr);
            if (Conn != null)
            {
                try
                {
                    MySqlDataAdapter Da1 = new MySqlDataAdapter(SQL1, Conn);
                    MySqlDataAdapter Da2 = new MySqlDataAdapter(SQL2, Conn);
                    DataSet ds = new DataSet();

                    Da1.Fill(ds, Table1);
                    Da2.Fill(ds, Table2);
                    var t1 = ds.Tables[Table1];
                    var t2 = ds.Tables[Table2];
                    if (t1 != null && t2 != null && t1.Rows.Count != 0 && t2.Rows.Count != 0)
                    {
                        DataColumn? parentCol = t1.Columns.Contains(Relation) ? t1.Columns[Relation] : null;
                        DataColumn? childCol = t2.Columns.Contains(Relation) ? t2.Columns[Relation] : null;
                        if (parentCol != null && childCol != null)
                        {
                            ds.Relations.Add(Discrible, parentCol, childCol);
                        }
                        Close_Conn(Conn);
                        return t1;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        // 运行MySql语句返回 MySqlDataReader对象
        public static MySqlDataReader Get_Reader(string SQL, string ConnStr)
        {
            MySqlConnection? Conn = Open_Conn(ConnStr);
            if (Conn == null)
            {
                throw new Exception("Failed to open MySQL connection.");
            }
            MySqlCommand Cmd = Create_Cmd(SQL, Conn);
            MySqlDataReader Dr;
            try
            {
                Dr = Cmd.ExecuteReader(CommandBehavior.Default);
            }
            catch
            {
                throw new Exception(SQL);
            }
            Close_Conn(Conn);
            return Dr;
        }

        // 运行MySql语句返回 MySqlDataAdapter对象 
        public static MySqlDataAdapter Get_Adapter(string SQL, string ConnStr)
        {
            MySqlConnection? Conn = Open_Conn(ConnStr);
            if (Conn == null)
            {
                throw new Exception("Failed to open MySQL connection.");
            }
            MySqlDataAdapter Da = new MySqlDataAdapter(SQL, Conn);
            return Da;
        }

        // 运行MySql语句,返回DataSet对象
        public static DataSet Get_DataSet(string SQL, string ConnStr, DataSet Ds)
        {
            MySqlConnection? Conn = Open_Conn(ConnStr);
            if (Conn == null)
            {
                throw new Exception("Failed to open MySQL connection.");
            }
            MySqlDataAdapter Da = new MySqlDataAdapter(SQL, Conn);
            try
            {
                Da.Fill(Ds);
            }
            catch (Exception)
            {
                throw;
            }
            Close_Conn(Conn);
            return Ds;
        }

        // 运行MySql语句,返回DataSet对象
        public static DataSet Get_DataSet(string SQL, string ConnStr, DataSet Ds, string tablename)
        {
            MySqlConnection? Conn = Open_Conn(ConnStr);
            if (Conn == null)
            {
                throw new Exception("Failed to open MySQL connection.");
            }
            MySqlDataAdapter Da = new MySqlDataAdapter(SQL, Conn);
            try
            {
                Da.Fill(Ds, tablename);
            }
            catch (Exception)
            {
                throw;
            }
            Close_Conn(Conn);
            return Ds;
        }

        // 运行MySql语句,返回DataSet对象，将数据进行了分页
        public static DataSet Get_DataSet(string SQL, string ConnStr, DataSet Ds, int StartIndex, int PageSize, string tablename)
        {
            MySqlConnection? Conn = Open_Conn(ConnStr);
            if (Conn == null)
            {
                throw new Exception("Failed to open MySQL connection.");
            }
            MySqlDataAdapter Da = new MySqlDataAdapter(SQL, Conn);
            try
            {
                Da.Fill(Ds, StartIndex, PageSize, tablename);
            }
            catch (Exception)
            {
                throw;
            }
            Close_Conn(Conn);
            return Ds;
        }

        // 返回MySql语句执行结果的第一行第一列
        public static string Get_Row1_Col1_Value(string SQL, string ConnStr)
        {
            MySqlConnection? Conn = Open_Conn(ConnStr);
            if (Conn == null)
            {
                throw new Exception("Failed to open MySQL connection.");
            }
            string result;
            MySqlDataReader Dr;
            try
            {
                Dr = Create_Cmd(SQL, Conn).ExecuteReader();
                if (Dr.Read())
                {
                    result = Convert.ToString(Dr[0]) ?? string.Empty;
                    Dr.Close();
                }
                else
                {
                    result = "";
                    Dr.Close();
                }
            }
            catch
            {
                throw new Exception(SQL);
            }
            Close_Conn(Conn);
            return result;
        }
    }
}
