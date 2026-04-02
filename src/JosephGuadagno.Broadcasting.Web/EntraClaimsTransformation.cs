using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace JosephGuadagno.Broadcasting.Web;

/// <summary>
/// Transforms claims from Entra ID authentication by adding user approval status and role claims
/// </summary>
public class EntraClaimsTransformation(
    IUserApprovalManager userApprovalManager,
    IRoleDataStore roleDataStore,
    ILogger<EntraClaimsTransformation> logger) : IClaimsTransformation
{
    private const string EntraObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string ApprovalStatusClaimType = "approval_status";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only process authenticated users
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        // Check if we've already transformed this principal (avoid duplicate processing)
        if (principal.HasClaim(c => c.Type == ApprovalStatusClaimType))
        {
            return principal;
        }

        // Extract Entra object ID
        var objectIdClaim = principal.FindFirst(EntraObjectIdClaimType);
        if (objectIdClaim is null)
        {
            logger.LogWarning("Entra object ID claim not found for authenticated user. Identity: {IdentityName}", 
                principal.Identity?.Name ?? "unknown");
            return principal;
        }

        var entraObjectId = objectIdClaim.Value;

        // Extract display name and email from existing claims for user creation
        var displayName = principal.FindFirst(ClaimTypes.Name)?.Value 
            ?? principal.FindFirst("name")?.Value 
            ?? "Unknown User";
        
        var email = principal.FindFirst(ClaimTypes.Email)?.Value 
            ?? principal.FindFirst("email")?.Value 
            ?? principal.FindFirst("preferred_username")?.Value 
            ?? "unknown@unknown.com";

        try
        {
            // Get or create user (auto-registers new users as Pending)
            var user = await userApprovalManager.GetOrCreateUserAsync(entraObjectId, displayName, email);

            // Create a new ClaimsIdentity with all existing claims plus new ones
            var claimsIdentity = new ClaimsIdentity(principal.Identity);

            // Add approval status claim
            claimsIdentity.AddClaim(new Claim(ApprovalStatusClaimType, user.ApprovalStatus));

            // Load and add role claims
            var roles = await roleDataStore.GetRolesForUserAsync(user.Id);
            foreach (var role in roles)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
            }

            logger.LogInformation(
                "Claims transformation completed for user {EntraObjectId}. ApprovalStatus: {ApprovalStatus}, Roles: {RoleCount}",
                entraObjectId, user.ApprovalStatus, roles.Count);

            // Return a new ClaimsPrincipal with the transformed identity
            return new ClaimsPrincipal(claimsIdentity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Failed to transform claims for Entra object ID {EntraObjectId}. Returning original principal.",
                entraObjectId);
            return principal;
        }
    }
}
