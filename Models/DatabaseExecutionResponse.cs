namespace DBAccess.Api.Models;

public class DatabaseExecutionResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public int? RowsAffected { get; set; }
}
