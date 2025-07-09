using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using IdentityServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// IdentityServer configuration
builder.Services.AddIdentityServer()
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients)
    .AddTestUsers(TestUsers.Users)
    .AddDeveloperSigningCredential(persistKey: true, filename: "ids_keys/tempkey.jwk"); // dev key persisted in volume

builder.Services.AddTransient<Duende.IdentityServer.Validation.IResourceOwnerPasswordValidator, PasswordValidator>();
builder.Services.AddSingleton<Duende.IdentityServer.Services.ICorsPolicyService>(sp => Cors.Build(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Duende.IdentityServer.Services.DefaultCorsPolicyService>>()));

var app = builder.Build();

app.UseRouting();

app.UseIdentityServer();

app.MapGet("/", () => "IdentityServer is running...");
app.MapGet("/healthz", () => Results.Ok("OK"));

app.Run(); 