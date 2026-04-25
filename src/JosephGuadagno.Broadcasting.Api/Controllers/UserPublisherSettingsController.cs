using AutoMapper;
using JosephGuadagno.Broadcasting.Api;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Manages per-user publisher settings for each social media platform.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class UserPublisherSettingsController(
    IUserPublisherSettingManager userPublisherSettingManager,
    ILogger<UserPublisherSettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets every publisher setting visible to the current caller.
    /// </summary>
    /// <param name="ownerOid">
    /// Optional Entra object ID to query. Non-admin callers can only query their own settings.
    /// </param>
    /// <returns>A list of publisher settings for the resolved owner.</returns>
    /// <response code="200">Returns the publisher settings for the resolved owner.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to query the requested owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<UserPublisherSettingResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<UserPublisherSettingResponse>>> GetAllAsync([FromQuery] string? ownerOid = null, int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "platform", bool sortDescending = false, string? filter = null)
    {
        if (page < 1) page = Pagination.DefaultPage;
        if (pageSize < 1 || pageSize > Pagination.MaxPageSize) pageSize = Pagination.DefaultPageSize;

        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        // TODO(morpheus): replace with GetByUserAsync(resolvedOwnerOid, page, pageSize, sortBy, sortDescending, filter) when paged overload is available
        var settings = await userPublisherSettingManager.GetByUserAsync(resolvedOwnerOid);
        var items = mapper.Map<List<UserPublisherSettingResponse>>(settings);
        return new PagedResponse<UserPublisherSettingResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = settings.Count
        };
    }

    /// <summary>
    /// Gets the publisher settings for a single social media platform.
    /// </summary>
    /// <param name="platformId">The social media platform identifier.</param>
    /// <param name="ownerOid">
    /// Optional Entra object ID to query. Non-admin callers can only query their own settings.
    /// </param>
    /// <returns>The publisher settings for the requested platform.</returns>
    /// <response code="200">Returns the publisher settings for the platform.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to query the requested owner.</response>
    /// <response code="404">No publisher settings exist for the resolved owner and platform.</response>
    [HttpGet("{platformId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserPublisherSettingResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPublisherSettingResponse>> GetAsync(int platformId, [FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var setting = await userPublisherSettingManager.GetByUserAndPlatformAsync(resolvedOwnerOid, platformId);
        if (setting is null)
        {
            logger.LogWarning(
                "User publisher settings not found for owner {OwnerOid} and platform {PlatformId}",
                LogSanitizer.Sanitize(resolvedOwnerOid),
                platformId);
            return NotFound();
        }

        return Ok(mapper.Map<UserPublisherSettingResponse>(setting));
    }

    /// <summary>
    /// Creates or updates the publisher settings for a social media platform.
    /// </summary>
    /// <param name="platformId">The social media platform identifier.</param>
    /// <param name="ownerOid">
    /// Optional Entra object ID to target. Non-admin callers can only save their own settings.
    /// </param>
    /// <param name="request">The publisher settings payload to save.</param>
    /// <returns>The saved publisher settings.</returns>
    /// <response code="200">Returns the saved publisher settings.</response>
    /// <response code="400">The request payload was invalid or the settings could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to save settings for the requested owner.</response>
    [HttpPut("{platformId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserPublisherSettingResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPublisherSettingResponse>> SaveAsync(
        int platformId,
        [FromQuery] string? ownerOid,
        [FromBody] UserPublisherSettingRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("SaveAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var setting = mapper.Map<UserPublisherSettingUpdate>(request);
        setting.CreatedByEntraOid = resolvedOwnerOid;
        setting.SocialMediaPlatformId = platformId;

        var saved = await userPublisherSettingManager.SaveAsync(setting);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to save user publisher settings for owner {OwnerOid} and platform {PlatformId}",
                LogSanitizer.Sanitize(resolvedOwnerOid),
                platformId);
            return BadRequest("Unable to save publisher settings");
        }

        return Ok(mapper.Map<UserPublisherSettingResponse>(saved));
    }

    /// <summary>
    /// Deletes the publisher settings for a social media platform.
    /// </summary>
    /// <param name="platformId">The social media platform identifier.</param>
    /// <param name="ownerOid">
    /// Optional Entra object ID to target. Non-admin callers can only delete their own settings.
    /// </param>
    /// <returns>No content when the delete succeeds.</returns>
    /// <response code="204">The publisher settings were deleted.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to delete settings for the requested owner.</response>
    /// <response code="404">No publisher settings exist for the resolved owner and platform.</response>
    [HttpDelete("{platformId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(int platformId, [FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var deleted = await userPublisherSettingManager.DeleteAsync(resolvedOwnerOid, platformId);
        if (!deleted)
        {
            logger.LogWarning(
                "User publisher settings not found for delete for owner {OwnerOid} and platform {PlatformId}",
                LogSanitizer.Sanitize(resolvedOwnerOid),
                platformId);
            return NotFound();
        }

        return NoContent();
    }

}
