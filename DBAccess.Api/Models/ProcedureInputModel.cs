using System.Text.Json.Serialization;

namespace DBAccess.Api.Models
{
    /// <summary>
    /// 存储过程输入参数模型
    /// </summary>
    public class ProcedureInputModel
    {
        /// <summary>
        /// 客户端IP地址
        /// </summary>
        /// <example>115.48.63.233</example>
        public string Client_IP { get; set; } = "";

        /// <summary>
        /// 域名
        /// </summary>
        /// <example>qsgl</example>
        public string domain { get; set; } = "";

        /// <summary>
        /// 子域名
        /// </summary>
        /// <example>3950</example>
        public string sub_domain { get; set; } = "";
    }
}