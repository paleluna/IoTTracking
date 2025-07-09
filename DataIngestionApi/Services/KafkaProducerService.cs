using System.Text.Json;
using Confluent.Kafka;
using DataIngestionApi.Models;
using DataIngestionApi.Options;
using Microsoft.Extensions.Options;

namespace DataIngestionApi.Services;

public class KafkaProducerService : IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaProducerService(IOptions<KafkaOptions> options)
    {
        var cfg = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = "data-ingestion-api"
        };
        _producer = new ProducerBuilder<string, string>(cfg).Build();
        _topic = options.Value.Topic;
    }

    public async Task ProduceAsync(SensorDataDto data, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(data);
        var message = new Message<string, string> { Key = data.DeviceId.ToString(), Value = json };
        await _producer.ProduceAsync(_topic, message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
        await ValueTask.CompletedTask;
    }
} 