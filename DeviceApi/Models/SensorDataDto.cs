namespace DeviceApi.Models;
using System;

public class SensorDataDto
{
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double? Humidity { get; set; }
} 