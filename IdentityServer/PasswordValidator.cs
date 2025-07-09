using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Test;
using System.Threading.Tasks;

namespace IdentityServer;

public class PasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly TestUserStore _users;

    public PasswordValidator(TestUserStore users)
    {
        _users = users;
    }

    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        var user = _users.FindByUsername(context.UserName);
        if (user != null && _users.ValidateCredentials(context.UserName, context.Password))
        {
            context.Result = new GrantValidationResult(
                subject: user.SubjectId,
                authenticationMethod: "password",
                claims: user.Claims);
        }
        else
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid credentials");
        }

        return Task.CompletedTask;
    }
} 