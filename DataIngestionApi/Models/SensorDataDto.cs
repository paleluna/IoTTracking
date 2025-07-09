namespace DataIngestionApi.Models;

public record SensorDataDto
{
    public int DeviceId { get; init; }
    public DateTime Timestamp { get; init; }
    public double Temperature { get; init; }
    public double Humidity { get; init; }
} 