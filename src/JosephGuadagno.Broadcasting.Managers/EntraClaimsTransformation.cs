using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Transforms claims from Entra ID authentication by adding user approval status and role claims.
/// Results are cached per user for <see cref="CacheTtl"/> to avoid a DB round-trip on every API request.
/// </summary>
public class EntraClaimsTransformation(
    IUserApprovalManager userApprovalManager,
    IMemoryCache cache,
    ILogger<EntraClaimsTransformation> logger) : IClaimsTransformation
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        if (principal.HasClaim(c => c.Type == ApplicationClaimTypes.ApprovalStatus))
        {
            return principal;
        }

        var objectIdClaim = principal.FindFirst(ApplicationClaimTypes.EntraObjectId)
            ?? principal.FindFirst(ApplicationClaimTypes.EntraObjectIdShort);
        if (objectIdClaim is null)
        {
            logger.LogWarning(
                "Entra object ID claim not found for authenticated user. Identity: {IdentityName}",
                principal.Identity?.Name ?? "unknown");

            return principal;
        }

        var entraObjectId = objectIdClaim.Value;

        if (cache.TryGetValue($"claims_{entraObjectId}", out CachedUserClaims? cached) && cached is not null)
        {
            return BuildPrincipal(principal, cached.ApprovalStatus, cached.ApprovalNotes, cached.Roles, cached.IsOnboarded);
        }

        var displayName = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("name")?.Value
            ?? "Unknown User";

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? "unknown@unknown.com";

        try
        {
            var user = await userApprovalManager.GetOrCreateUserAsync(entraObjectId, displayName, email);
            var roles = await userApprovalManager.GetUserRolesAsync(user.Id);

            var entry = new CachedUserClaims(
                user.ApprovalStatus,
                user.ApprovalStatus == nameof(ApprovalStatus.Rejected) ? user.ApprovalNotes : null,
                roles.Select(r => r.Name).ToList(),
                user.IsOnboarded);

            cache.Set($"claims_{entraObjectId}", entry, CacheTtl);

            logger.LogInformation(
                "Claims transformation completed for user {EntraObjectId}. ApprovalStatus: {ApprovalStatus}, Roles: {RoleCount}, IsOnboarded: {IsOnboarded}",
                entraObjectId,
                user.ApprovalStatus,
                roles.Count,
                user.IsOnboarded);

            return BuildPrincipal(principal, entry.ApprovalStatus, entry.ApprovalNotes, entry.Roles, entry.IsOnboarded);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to transform claims for Entra object ID {EntraObjectId}. Returning original principal",
                entraObjectId);

            return principal;
        }
    }

    private static ClaimsPrincipal BuildPrincipal(
        ClaimsPrincipal principal,
        string approvalStatus,
        string? approvalNotes,
        IEnumerable<string> roles,
        bool isOnboarded)
    {
        var claimsIdentity = new ClaimsIdentity(principal.Identity);
        claimsIdentity.AddClaim(new Claim(ApplicationClaimTypes.ApprovalStatus, approvalStatus));
        claimsIdentity.AddClaim(new Claim(ApplicationClaimTypes.IsOnboarded, isOnboarded ? "true" : "false"));

        if (!string.IsNullOrWhiteSpace(approvalNotes))
        {
            claimsIdentity.AddClaim(new Claim(ApplicationClaimTypes.ApprovalNotes, approvalNotes));
        }

        foreach (var role in roles)
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(claimsIdentity);
    }

    private sealed record CachedUserClaims(string ApprovalStatus, string? ApprovalNotes, List<string> Roles, bool IsOnboarded);
}
