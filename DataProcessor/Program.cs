using DataProcessor.Options;
using DataProcessor.Workers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<KafkaOptions>(hostContext.Configuration.GetSection("Kafka"));
        services.Configure<DatabaseOptions>(hostContext.Configuration.GetSection("Database"));
        services.AddHostedService<SensorDataWorker>();
    });

// Добавляем запуск Kestrel и endpoint healthz
builder.ConfigureWebHostDefaults(webBuilder =>
{
    webBuilder.Configure(app =>
    {
        app.MapGet("/healthz", () => "OK");
    });
});

var host = builder.Build();

await host.RunAsync();