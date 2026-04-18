using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Tests.Helpers;

/// <summary>
/// Shared controller-context factory for API controller unit tests.
/// Centralizes the claim-building logic so individual test classes do not
/// need to duplicate it.
/// </summary>
public static class ApiControllerTestHelpers
{
    /// <summary>
    /// Builds an <see cref="HttpContext"/> whose <see cref="ClaimsPrincipal"/> carries the
    /// given OAuth scope so that <c>HttpContext.VerifyUserHasAnyAcceptedScope</c> succeeds.
    /// Both the short "scp" claim and the full URI claim type are set for maximum
    /// compatibility with different versions of Microsoft.Identity.Web.
    /// </summary>
    public static ControllerContext CreateControllerContext(
        string scopeClaimValue,
        string ownerOid = "owner-oid-12345",
        bool isSiteAdmin = false)
    {
        var claims = new List<Claim>
        {
            new Claim("scp", scopeClaimValue),
            new Claim("http://schemas.microsoft.com/identity/claims/scope", scopeClaimValue),
            new Claim(Domain.Constants.ApplicationClaimTypes.EntraObjectId, ownerOid)
        };
        if (isSiteAdmin)
            claims.Add(new Claim(ClaimTypes.Role, Domain.Constants.RoleNames.SiteAdministrator));

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
        var httpContext = new DefaultHttpContext { User = user };
        return new ControllerContext { HttpContext = httpContext };
    }

    /// <summary>
    /// Creates a controller context where the user OID does NOT match the entity's
    /// <c>CreatedByEntraOid</c>.
    /// Use for testing ownership rejection (403 Forbid).
    /// </summary>
    public static ControllerContext CreateNonOwnerControllerContext(string scopeClaimValue) =>
        CreateControllerContext(scopeClaimValue, ownerOid: "non-owner-oid-99999");
}
