using System.Text.Json;
using Confluent.Kafka;
using Dapper;
using DataProcessor.Models;
using DataProcessor.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DataProcessor.Workers;

public class SensorDataWorker : BackgroundService
{
    private readonly ILogger<SensorDataWorker> _logger;
    private readonly KafkaOptions _kafkaOptions;
    private readonly DatabaseOptions _dbOptions;

    private IConsumer<string, string>? _consumer;
    private NpgsqlConnection? _timescaleConn;
    private NpgsqlConnection? _postgresConn;

    public SensorDataWorker(
        ILogger<SensorDataWorker> logger,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<DatabaseOptions> dbOptions)
    {
        _logger = logger;
        _kafkaOptions = kafkaOptions.Value;
        _dbOptions = dbOptions.Value;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SensorDataWorker");

        // Initialize Kafka consumer
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.GroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true
        };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(_kafkaOptions.Topic);

        // Initialize database connections
        _timescaleConn = new NpgsqlConnection(_dbOptions.TimescaleConnectionString);
        _postgresConn = new NpgsqlConnection(_dbOptions.PostgresConnectionString);

        await _timescaleConn.OpenAsync(cancellationToken);
        await _postgresConn.OpenAsync(cancellationToken);

        await EnsureSchemaAsync();

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SensorDataWorker running");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer!.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                    continue;

                var message = result.Message.Value;
                var sensorData = JsonSerializer.Deserialize<SensorDataDto>(message);
                if (sensorData is null)
                {
                    _logger.LogWarning("Received null/invalid sensor data JSON: {Json}", message);
                    continue;
                }

                await InsertTimescaleAsync(sensorData);
                // При необходимости можно писать метаданные в PostgreSQL
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing sensor data");
            }
        }
    }

    private async Task InsertTimescaleAsync(SensorDataDto data)
    {
        const string sql = "INSERT INTO sensor_data(device_id, timestamp, temperature, humidity) VALUES (@DeviceId, @Timestamp, @Temperature, @Humidity);";
        await _timescaleConn!.ExecuteAsync(sql, new
        {
            DeviceId = data.DeviceId,
            Timestamp = data.Timestamp,
            Temperature = data.Temperature,
            Humidity = data.Humidity
        });
    }

    private async Task EnsureSchemaAsync()
    {
        const string createTable = @"CREATE TABLE IF NOT EXISTS sensor_data (
            device_id     INTEGER       NOT NULL,
            timestamp     TIMESTAMPTZ   NOT NULL,
            temperature   DOUBLE PRECISION,
            humidity      DOUBLE PRECISION,
            PRIMARY KEY (device_id, timestamp)
        );";

        const string createHyper = "SELECT create_hypertable('sensor_data','timestamp', if_not_exists => TRUE);";

        await _timescaleConn!.ExecuteAsync(createTable);
        await _timescaleConn.ExecuteAsync(createHyper);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SensorDataWorker");
        _consumer?.Close();
        _timescaleConn?.Close();
        _postgresConn?.Close();
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        _timescaleConn?.Dispose();
        _postgresConn?.Dispose();
        base.Dispose();
    }
} 