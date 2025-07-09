using System.Collections.Generic;
using Duende.IdentityServer.Models;
using System;

namespace IdentityServer;

public static class Config
{
    public static IEnumerable<ApiScope> ApiScopes => new List<ApiScope>
    {
        new("api", "Main API")
    };

    public static IEnumerable<ApiResource> ApiResources => new List<ApiResource>
    {
        new("api", "Main API") { Scopes = { "api" } }
    };

    public static IEnumerable<Client> Clients => new List<Client>
    {
        // Machine-to-machine client (microservices)
        new Client
        {
            ClientId = "service-client",
            ClientName = "Microservice Client",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret((System.Environment.GetEnvironmentVariable("SERVICE_CLIENT_SECRET") ?? "service-secret").Sha256()) },
            AllowedScopes = { "api" }
        },
        // WebApp using ROPC for simplicity in MVP
        new Client
        {
            ClientId = "webapp",
            ClientName = "React WebApp",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            ClientSecrets = { new Secret((System.Environment.GetEnvironmentVariable("WEBAPP_CLIENT_SECRET") ?? "webapp-secret").Sha256()) },
            AllowedScopes = { "api" },
            AllowOfflineAccess = true
        }
    };
} 