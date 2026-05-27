using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers.Dispatchers;

/// <summary>
/// Manages per-user LinkedIn publisher settings.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Dispatchers/LinkedIn")]
[Produces("application/json")]
public class LinkedInSettingsController(
    IUserPlatformLinkedInSettingsManager manager,
    IOnboardingManager onboardingManager,
    ILogger<LinkedInSettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets the LinkedIn publisher settings for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own settings.</param>
    /// <returns>The LinkedIn settings for the resolved owner, or no content if not yet configured.</returns>
    /// <response code="200">Returns the LinkedIn publisher settings.</response>
    /// <response code="204">No LinkedIn settings exist for the resolved owner yet.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to query the requested owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LinkedInSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<LinkedInSettingsResponse>> GetAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var settings = await manager.GetAsync(resolvedOwnerOid);
        if (settings is null)
        {
            logger.LogInformation("LinkedIn settings not yet configured for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NoContent();
        }

        return Ok(mapper.Map<LinkedInSettingsResponse>(settings));
    }

    /// <summary>
    /// Creates or updates the LinkedIn publisher settings for the resolved owner.
    /// </summary>
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LinkedInSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LinkedInSettingsResponse>> SaveAsync(
        [FromQuery] string? ownerOid,
        [FromBody] LinkedInSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var settings = await manager.GetAsync(resolvedOwnerOid)
            ?? new UserPlatformLinkedInSettings { CreatedByEntraOid = resolvedOwnerOid };

        settings.IsEnabled = request.IsEnabled;
        settings.AuthorId = request.AuthorId;
        settings.ClientId = request.ClientId;

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            await manager.StoreClientSecretAsync(resolvedOwnerOid, request.ClientSecret);
            settings.HasClientSecret = true;
        }

        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            await manager.StoreAccessTokenAsync(resolvedOwnerOid, request.AccessToken);
        }

        var saved = await manager.SaveAsync(settings);
        if (saved is null)
        {
            logger.LogWarning("Failed to save LinkedIn settings for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return BadRequest("Unable to save LinkedIn publisher settings");
        }

        await onboardingManager.RecalculateAsync(resolvedOwnerOid);
        return Ok(mapper.Map<LinkedInSettingsResponse>(saved));
    }

    /// <summary>
    /// Deletes the LinkedIn publisher settings for the resolved owner.
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var deleted = await manager.DeleteAsync(resolvedOwnerOid);
        if (!deleted)
        {
            logger.LogWarning("LinkedIn settings not found for delete for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NotFound();
        }

        await onboardingManager.RecalculateAsync(resolvedOwnerOid);
        return NoContent();
    }
}

