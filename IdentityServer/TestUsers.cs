using Duende.IdentityServer.Test;
using System.Security.Claims;
using System.Collections.Generic;

namespace IdentityServer;

public static class TestUsers
{
    public static List<TestUser> Users => new()
    {
        new TestUser
        {
            SubjectId = "1",
            Username = "admin",
            Password = "admin",
            Claims = new List<Claim>
            {
                new("role", "admin")
            }
        }
    };
} 