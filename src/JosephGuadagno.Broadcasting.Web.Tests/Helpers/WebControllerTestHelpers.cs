using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Web.Tests.Helpers;

/// <summary>
/// Shared controller-context factory for Web MVC controller unit tests.
/// Centralizes the claim-building logic so individual test classes do not
/// need to duplicate it.
/// </summary>
public static class WebControllerTestHelpers
{
    /// <summary>
    /// Builds a <see cref="ControllerContext"/> whose <see cref="ClaimsPrincipal"/>
    /// carries the given <paramref name="ownerOid"/> and optional <paramref name="role"/>.
    /// </summary>
    public static ControllerContext CreateControllerContext(
        string ownerOid,
        string role = RoleNames.Contributor)
    {
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, ownerOid),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    /// <summary>
    /// Creates a controller context where the user OID does NOT match the entity's
    /// <c>CreatedByEntraOid</c>.  Use for testing ownership rejection scenarios
    /// (Web MVC redirects with an error message rather than returning ForbidResult).
    /// </summary>
    public static ControllerContext CreateNonOwnerControllerContext(string role = RoleNames.Contributor) =>
        CreateControllerContext(ownerOid: "non-owner-oid-99999", role: role);
}
