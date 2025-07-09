namespace DeviceApi.Models;
using System;

public record SensorDataDto
{
    public DateTime Timestamp { get; init; }
    public double   Temperature { get; init; }
} 