using System.ComponentModel.DataAnnotations;

namespace DBAccess.Api.Models;

public class ProcedureRequest
{
    [Required]
    public string DBName { get; set; } = string.Empty;
    [Required]
    public string ProcedureName { get; set; } = string.Empty;
    [Required]
    public string[] InputName { get; set; } = Array.Empty<string>();
    [Required]
    public string[] InputValue { get; set; } = Array.Empty<string>();
}

public class FunctionRequest
{
    [Required]
    public string DBName { get; set; } = string.Empty;
    [Required]
    public string FunctionName { get; set; } = string.Empty;
    [Required]
    public string InputValue { get; set; } = string.Empty;
    public string Fields { get; set; } = "*";
    public string WhereStr { get; set; } = string.Empty;
    public string OrderStr { get; set; } = string.Empty;
    public string Limit { get; set; } = string.Empty;
    public string Offset { get; set; } = "0";
}

public class TableRequest
{
    [Required]
    public string DBName { get; set; } = string.Empty;
    [Required]
    public string TableName { get; set; } = string.Empty;
    public string WhereStr { get; set; } = string.Empty;
    public string Fields { get; set; } = "*";
    public string OrderStr { get; set; } = string.Empty;
    public string Limit { get; set; } = string.Empty;
    public string Offset { get; set; } = "0";
}
