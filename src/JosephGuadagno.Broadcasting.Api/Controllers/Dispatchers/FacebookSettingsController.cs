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
/// Manages per-user Facebook publisher settings.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Dispatchers/Facebook")]
[Produces("application/json")]
public class FacebookSettingsController(
    IUserPublisherFacebookSettingsManager manager,
    IOnboardingManager onboardingManager,
    ILogger<FacebookSettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets the Facebook publisher settings for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own settings.</param>
    /// <returns>The Facebook settings for the resolved owner, or no content if not yet configured.</returns>
    /// <response code="200">Returns the Facebook publisher settings.</response>
    /// <response code="204">No Facebook settings exist for the resolved owner yet.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to query the requested owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FacebookSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<FacebookSettingsResponse>> GetAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var settings = await manager.GetAsync(resolvedOwnerOid);
        if (settings is null)
        {
            logger.LogInformation("Facebook settings not yet configured for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NoContent();
        }

        return Ok(mapper.Map<FacebookSettingsResponse>(settings));
    }

    /// <summary>
    /// Creates or updates the Facebook publisher settings for the resolved owner.
    /// </summary>
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FacebookSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FacebookSettingsResponse>> SaveAsync(
        [FromQuery] string? ownerOid,
        [FromBody] FacebookSettingsRequest request)
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
            ?? new UserPublisherFacebookSettings { CreatedByEntraOid = resolvedOwnerOid };

        settings.IsEnabled = request.IsEnabled;
        settings.PageId = request.PageId;
        settings.AppId = request.AppId;

        if (!string.IsNullOrWhiteSpace(request.PageAccessToken))
        {
            await manager.StorePageAccessTokenAsync(resolvedOwnerOid, request.PageAccessToken);
            settings.HasPageAccessToken = true;
        }

        if (!string.IsNullOrWhiteSpace(request.AppSecret))
        {
            await manager.StoreAppSecretAsync(resolvedOwnerOid, request.AppSecret);
            settings.HasAppSecret = true;
        }

        if (!string.IsNullOrWhiteSpace(request.ClientToken))
        {
            await manager.StoreClientTokenAsync(resolvedOwnerOid, request.ClientToken);
            settings.HasClientToken = true;
        }

        if (!string.IsNullOrWhiteSpace(request.ShortLivedAccessToken))
        {
            await manager.StoreShortLivedAccessTokenAsync(resolvedOwnerOid, request.ShortLivedAccessToken);
            settings.HasShortLivedAccessToken = true;
        }

        if (!string.IsNullOrWhiteSpace(request.LongLivedAccessToken))
        {
            await manager.StoreLongLivedAccessTokenAsync(resolvedOwnerOid, request.LongLivedAccessToken);
            settings.HasLongLivedAccessToken = true;
        }

        var saved = await manager.SaveAsync(settings);
        if (saved is null)
        {
            logger.LogWarning("Failed to save Facebook settings for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return BadRequest("Unable to save Facebook publisher settings");
        }

        await onboardingManager.RecalculateAsync(resolvedOwnerOid);
        return Ok(mapper.Map<FacebookSettingsResponse>(saved));
    }

    /// <summary>
    /// Deletes the Facebook publisher settings for the resolved owner.
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
            logger.LogWarning("Facebook settings not found for delete for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NotFound();
        }

        await onboardingManager.RecalculateAsync(resolvedOwnerOid);
        return NoContent();
    }
}
