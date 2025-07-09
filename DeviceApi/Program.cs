using Serilog;
using DeviceApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// DB connection string via env
var connStr = builder.Configuration.GetValue<string>("ConnectionStrings:Default") ?? "Host=postgres;Database=iot;Username=user;Password=pass";
builder.Services.AddSingleton(new DeviceRepository(connStr));

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