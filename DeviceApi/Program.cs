using Serilog;
using DeviceApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// DB connection string via env
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddSingleton(sp => new DeviceRepository(
    connectionString, 
    sp.GetRequiredService<ILogger<DeviceRepository>>())
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ───────── Swagger ─────────
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/healthz", () => Results.Ok("OK"));
app.Run(); 