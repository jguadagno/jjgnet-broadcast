using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers.Platforms;

/// <summary>
/// Manages per-user Bluesky publisher settings.
/// </summary>
[ApiController]
[Tags("Platforms")]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Platforms/Bluesky")]
[Produces("application/json")]
public class BlueskySettingsController(
    IUserPlatformBlueskySettingsManager manager,
    IOnboardingManager onboardingManager,
    ILogger<BlueskySettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets the Bluesky publisher settings for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own settings.</param>
    /// <response code="200">Returns the Bluesky settings.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller may not query the requested owner.</response>
    /// <response code="204">No Bluesky settings for the resolved owner yet.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BlueskySettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<BlueskySettingsResponse>> GetAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var settings = await manager.GetAsync(resolvedOwnerOid);
        if (settings is null)
        {
            logger.LogInformation("Bluesky settings not yet configured for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NoContent();
        }

        return Ok(mapper.Map<BlueskySettingsResponse>(settings));
    }

    /// <summary>
    /// Creates or updates the Bluesky publisher settings for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only update their own settings.</param>
    /// <param name="request">The Bluesky settings payload.</param>
    /// <response code="200">Returns the saved Bluesky settings.</response>
    /// <response code="400">Invalid request or save failed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller may not update settings for the requested owner.</response>
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BlueskySettingsResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BlueskySettingsResponse>> SaveAsync(
        [FromQuery] string? ownerOid,
        [FromBody] BlueskySettingsRequest request)
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
            ?? new UserPlatformBlueskySettings { CreatedByEntraOid = resolvedOwnerOid };

        settings.IsEnabled = request.IsEnabled;
        settings.UserName = request.UserName;

        if (!string.IsNullOrWhiteSpace(request.AppPassword))
        {
            await manager.StoreAppPasswordAsync(resolvedOwnerOid, request.AppPassword);
            settings.HasAppPassword = true;
        }

        var saved = await manager.SaveAsync(settings);
        if (saved is null)
        {
            logger.LogWarning("Failed to save Bluesky settings for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return BadRequest("Unable to save Bluesky publisher settings");
        }

        await onboardingManager.RecalculateAsync(resolvedOwnerOid);
        return Ok(mapper.Map<BlueskySettingsResponse>(saved));
    }

    /// <summary>
    /// Deletes the Bluesky publisher settings for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only delete their own settings.</param>
    /// <response code="204">Settings deleted.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller may not delete settings for the requested owner.</response>
    /// <response code="404">No Bluesky settings found for the resolved owner.</response>
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
            logger.LogWarning("Bluesky settings not found for delete for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NotFound();
        }

        await onboardingManager.RecalculateAsync(resolvedOwnerOid);
        return NoContent();
    }
}

