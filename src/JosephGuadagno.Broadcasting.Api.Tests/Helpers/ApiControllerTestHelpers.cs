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
    /// role claims needed by the API authorization policies and the current owner OID.
    /// Tests can elevate to site administrator by setting <paramref name="isSiteAdmin"/>.
    /// </summary>
    public static ControllerContext CreateControllerContext(
        string roleName = Domain.Constants.RoleNames.Contributor,
        string ownerOid = "owner-oid-12345",
        bool isSiteAdmin = false)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, roleName),
            new Claim(Domain.Constants.ApplicationClaimTypes.EntraObjectId, ownerOid)
        };
        if (isSiteAdmin && roleName != Domain.Constants.RoleNames.SiteAdministrator)
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
    public static ControllerContext CreateNonOwnerControllerContext(string roleName = Domain.Constants.RoleNames.Contributor) =>
        CreateControllerContext(roleName, ownerOid: "non-owner-oid-99999");
}
