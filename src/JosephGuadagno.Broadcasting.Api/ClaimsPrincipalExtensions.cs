using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Api;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> that resolve owner OID and role helpers
/// used across API controllers.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the caller's Entra Object ID from claims.
    /// Checks the full URI claim first, then the short "oid" form used by newer JWT handlers.
    /// </summary>
    /// <param name="principal">The current user principal.</param>
    /// <returns>The Entra Object ID string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither the full-URI nor the short-form OID claim is present.
    /// </exception>
    public static string GetOwnerOid(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ApplicationClaimTypes.EntraObjectId)
           ?? principal.FindFirstValue(ApplicationClaimTypes.EntraObjectIdShort)
           ?? throw new InvalidOperationException("Entra Object ID claim not found");

    /// <summary>
    /// Returns <c>true</c> when the caller holds the <see cref="RoleNames.SiteAdministrator"/> role.
    /// </summary>
    /// <param name="principal">The current user principal.</param>
    public static bool IsSiteAdministrator(this ClaimsPrincipal principal)
        => principal.IsInRole(RoleNames.SiteAdministrator);

    /// <summary>
    /// Resolves the effective owner OID for the request.
    /// <list type="bullet">
    ///   <item>If <paramref name="requestedOwnerOid"/> is null/empty, or equals the caller's own OID, the caller's OID is returned.</item>
    ///   <item>If <paramref name="requestedOwnerOid"/> targets a different user and <paramref name="requireAdminWhenTargetingOtherUser"/> is <c>true</c>, returns <c>null</c> for non-admins (caller should respond with 403).</item>
    ///   <item>Site administrators may target any OID when <paramref name="requireAdminWhenTargetingOtherUser"/> is <c>true</c>.</item>
    /// </list>
    /// </summary>
    /// <param name="principal">The current user principal.</param>
    /// <param name="requestedOwnerOid">The OID supplied by the caller (e.g. from a query-string parameter).</param>
    /// <param name="requireAdminWhenTargetingOtherUser">
    /// When <c>true</c>, non-admin callers who specify a different user's OID receive <c>null</c> (forbidden signal).
    /// </param>
    /// <returns>
    /// The resolved owner OID, or <c>null</c> when the caller is not authorised to target the requested OID.
    /// </returns>
    public static string? ResolveOwnerOid(
        this ClaimsPrincipal principal,
        string? requestedOwnerOid,
        bool requireAdminWhenTargetingOtherUser = true)
    {
        var currentOwnerOid = principal.GetOwnerOid();

        if (string.IsNullOrWhiteSpace(requestedOwnerOid)
            || string.Equals(requestedOwnerOid, currentOwnerOid, StringComparison.OrdinalIgnoreCase))
        {
            return currentOwnerOid;
        }

        return requireAdminWhenTargetingOtherUser && !principal.IsSiteAdministrator()
            ? null
            : requestedOwnerOid;
    }
}
