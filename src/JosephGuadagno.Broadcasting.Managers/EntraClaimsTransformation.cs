using System;
using System.Security.Claims;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Transforms claims from Entra ID authentication by adding user approval status and role claims.
/// </summary>
public class EntraClaimsTransformation(
    IUserApprovalManager userApprovalManager,
    ILogger<EntraClaimsTransformation> logger) : IClaimsTransformation
{
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

        var objectIdClaim = principal.FindFirst(ApplicationClaimTypes.EntraObjectId);
        if (objectIdClaim is null)
        {
            logger.LogWarning(
                "Entra object ID claim not found for authenticated user. Identity: {IdentityName}",
                principal.Identity?.Name ?? "unknown");

            return principal;
        }

        var entraObjectId = objectIdClaim.Value;
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

            var claimsIdentity = new ClaimsIdentity(principal.Identity);
            claimsIdentity.AddClaim(new Claim(ApplicationClaimTypes.ApprovalStatus, user.ApprovalStatus));

            if (user.ApprovalStatus == nameof(ApprovalStatus.Rejected) &&
                !string.IsNullOrWhiteSpace(user.ApprovalNotes))
            {
                claimsIdentity.AddClaim(new Claim(ApplicationClaimTypes.ApprovalNotes, user.ApprovalNotes));
            }

            var roles = await userApprovalManager.GetUserRolesAsync(user.Id);
            foreach (var role in roles)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
            }

            logger.LogInformation(
                "Claims transformation completed for user {EntraObjectId}. ApprovalStatus: {ApprovalStatus}, Roles: {RoleCount}",
                entraObjectId,
                user.ApprovalStatus,
                roles.Count);

            return new ClaimsPrincipal(claimsIdentity);
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
}
