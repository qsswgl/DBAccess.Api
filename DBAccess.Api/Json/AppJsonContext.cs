using DBAccess.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DBAccess.Api.Json
{
    [JsonSerializable(typeof(ProcedureInputModel))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(ProblemDetails))]
    [JsonSerializable(typeof(ValidationProblemDetails))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}