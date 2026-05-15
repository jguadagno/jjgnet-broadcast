using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers.Publishers;

/// <summary>
/// Manages per-user LinkedIn publisher settings.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Publishers/LinkedIn")]
[Produces("application/json")]
public class LinkedInSettingsController(
    IUserPublisherLinkedInSettingsManager manager,
    ILogger<LinkedInSettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets the LinkedIn publisher settings for the resolved owner.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LinkedInSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            logger.LogWarning("LinkedIn settings not found for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NotFound();
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
            ?? new UserPublisherLinkedInSettings { CreatedByEntraOid = resolvedOwnerOid };

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
            settings.HasAccessToken = true;
        }

        var saved = await manager.SaveAsync(settings);
        if (saved is null)
        {
            logger.LogWarning("Failed to save LinkedIn settings for owner '{OwnerOid}'", LogSanitizer.Sanitize(resolvedOwnerOid));
            return BadRequest("Unable to save LinkedIn publisher settings");
        }

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

        return NoContent();
    }
}
