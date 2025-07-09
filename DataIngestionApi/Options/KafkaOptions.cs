namespace DataIngestionApi.Options;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string Topic { get; set; } = "sensor-data";
} 