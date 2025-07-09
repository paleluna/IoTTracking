using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using MMLib.SwaggerForOcelot;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.Multiplexer;
using ApiGateway.Aggregators;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
       .AddJsonFile("ocelot.SwaggerEndPoints.json", optional: false, reloadOnChange: true);

// IdentityServer URL (docker service name "identity")
var identityUrl = builder.Configuration["IdentityUrl"] ?? "http://identity";

// CORS: allow requests from the React frontend (localhost:3000) or any origin in dev
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer("Bearer", options =>
{
    options.Authority = identityUrl;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false,
        ValidateIssuer = false
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerForOcelot(builder.Configuration, o =>
{
    // Включаем автоматическую генерацию Swagger-страницы для агрегатных маршрутов
    o.GenerateDocsForAggregates = true;
});

// регистрируем агрегатор чтобы Ocelot смог его найти через DI
builder.Services.AddSingleton<IDefinedAggregator, DeviceSummaryAggregator>();

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// enable CORS before auth so that the browser gets the headers even on 401/403
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Swagger UI со списком всех downstream-сервисов
app.UseSwaggerForOcelotUI(opts =>
{
    opts.PathToSwaggerGenerator = "/swagger/docs";
});

await app.UseOcelot();

app.MapGet("/healthz", () => Results.Ok("OK"));

app.Run(); 