namespace DataProcessor.Options;

public class DatabaseOptions
{
    public string TimescaleConnectionString { get; set; } = string.Empty;
    public string PostgresConnectionString { get; set; } = string.Empty;
} 