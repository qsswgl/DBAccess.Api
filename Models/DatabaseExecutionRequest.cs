namespace DBAccess.Api.Models;

public class DatabaseExecutionRequest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?>? Parameters { get; set; }
}
