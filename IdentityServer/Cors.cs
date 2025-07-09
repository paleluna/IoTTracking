using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;

namespace IdentityServer;

public static class Cors
{
    public static ICorsPolicyService Build(ILogger<DefaultCorsPolicyService> logger)
    {
        return new DefaultCorsPolicyService(logger)
        {
            AllowedOrigins = { "http://localhost:3000" },
            AllowAll = false
        };
    }
} 