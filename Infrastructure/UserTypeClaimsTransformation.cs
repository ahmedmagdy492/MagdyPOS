using System.Security.Claims;
using MagdyPOS.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace MagdyPOS.Infrastructure;

public sealed class UserTypeClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        if (identity.HasClaim(c => c.Type == AppClaims.UserType))
        {
            return Task.FromResult(principal);
        }

        var userType = principal.IsInRole(AppRoles.Admin) ? "admin" : "sales";
        identity.AddClaim(new Claim(AppClaims.UserType, userType));
        return Task.FromResult(principal);
    }
}
