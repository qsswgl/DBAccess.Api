using DBAccess.Api.Models;
using System.Text.Json.Serialization;

namespace DBAccess.Api.Json
{
    [JsonSerializable(typeof(ProcedureInputModel))]
    [JsonSerializable(typeof(string))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}