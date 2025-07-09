namespace DataProcessor.Options;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string Topic { get; set; } = "sensor-data";
    public string GroupId { get; set; } = "data-processor";
} 