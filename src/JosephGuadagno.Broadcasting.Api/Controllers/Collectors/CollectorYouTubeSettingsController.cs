using AutoMapper;
using JosephGuadagno.Broadcasting.Api;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers.Collectors;

/// <summary>
/// Manages per-user YouTube channel collector configurations under the
/// <c>/Collectors/YouTube/Settings</c> route.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Collectors/YouTube/Settings")]
[Produces("application/json")]
public class CollectorYouTubeSettingsController(
    IUserCollectorYouTubeChannelManager youTubeChannelManager,
    ILogger<CollectorYouTubeSettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets a paged list of YouTube channel configurations for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own configurations.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 25).</param>
    /// <param name="sortBy">The field to sort by (default: channelname).</param>
    /// <param name="sortDescending">When true, sorts in descending order (default: false).</param>
    /// <param name="filter">Optional text filter applied to YouTube channel names.</param>
    /// <returns>A paged list of YouTube channel configurations for the resolved owner.</returns>
    /// <response code="200">Returns the YouTube channel configurations for the resolved owner.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to query the requested owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<UserCollectorYouTubeChannelResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<UserCollectorYouTubeChannelResponse>>> GetAllAsync(
        [FromQuery] string? ownerOid = null,
        int page = Pagination.DefaultPage,
        int pageSize = Pagination.DefaultPageSize,
        string sortBy = "channelname",
        bool sortDescending = false,
        string? filter = null)
    {
        if (page < 1) page = Pagination.DefaultPage;
        if (pageSize < 1 || pageSize > Pagination.MaxPageSize) pageSize = Pagination.DefaultPageSize;

        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var result = await youTubeChannelManager.GetAllAsync(resolvedOwnerOid, page, pageSize, sortBy, sortDescending, filter);
        var items = mapper.Map<List<UserCollectorYouTubeChannelResponse>>(result.Items);
        return new PagedResponse<UserCollectorYouTubeChannelResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Gets a YouTube channel configuration by ID.
    /// </summary>
    /// <param name="id">The configuration identifier.</param>
    /// <returns>The YouTube channel configuration.</returns>
    /// <response code="200">Returns the YouTube channel configuration.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to access this configuration.</response>
    /// <response code="404">No configuration exists with the specified ID.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorYouTubeChannelResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCollectorYouTubeChannelResponse>> GetAsync(int id)
    {
        var config = await youTubeChannelManager.GetByIdAsync(id);
        if (config is null)
        {
            logger.LogWarning("YouTube channel config not found for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();

        if (User.ResolveOwnerOid(config.CreatedByEntraOid, requireAdminWhenTargetingOtherUser: true) is null)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to access YouTube channel config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(config.CreatedByEntraOid));
            return Forbid();
        }

        return Ok(mapper.Map<UserCollectorYouTubeChannelResponse>(config));
    }

    /// <summary>
    /// Creates a YouTube channel configuration.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only create configurations for themselves.</param>
    /// <param name="request">The YouTube channel configuration payload to create.</param>
    /// <returns>The created YouTube channel configuration.</returns>
    /// <response code="200">Returns the created YouTube channel configuration.</response>
    /// <response code="400">The request payload was invalid or the configuration could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to create configurations for the requested owner.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorYouTubeChannelResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserCollectorYouTubeChannelResponse>> SaveAsync(
        [FromQuery] string? ownerOid,
        [FromBody] CreateUserCollectorYouTubeChannelRequest request)
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

        var config = mapper.Map<UserCollectorYouTubeChannel>(request);
        config.CreatedByEntraOid = resolvedOwnerOid;

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            await youTubeChannelManager.StoreApiKeyToKeyVaultAsync(
                resolvedOwnerOid,
                request.ChannelId,
                request.ApiKey);
        }

        var saved = await youTubeChannelManager.SaveAsync(config);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to save YouTube channel config for owner {OwnerOid} and channel ID {ChannelId}",
                LogSanitizer.Sanitize(resolvedOwnerOid),
                LogSanitizer.Sanitize(request.ChannelId));
            return BadRequest("Unable to save YouTube channel configuration");
        }

        return Ok(mapper.Map<UserCollectorYouTubeChannelResponse>(saved));
    }

    /// <summary>
    /// Updates an existing YouTube channel configuration.
    /// </summary>
    /// <param name="id">The configuration identifier.</param>
    /// <param name="request">The YouTube channel configuration payload.</param>
    /// <returns>The updated YouTube channel configuration.</returns>
    /// <response code="200">Returns the updated YouTube channel configuration.</response>
    /// <response code="400">The request payload was invalid or the configuration could not be updated.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to update this configuration.</response>
    /// <response code="404">No configuration exists with the specified ID.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorYouTubeChannelResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCollectorYouTubeChannelResponse>> UpdateAsync(
        int id,
        [FromBody] UpdateUserCollectorYouTubeChannelRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("UpdateAsync called with invalid model state for ID {Id}", id);
            return BadRequest(ModelState);
        }

        var existing = await youTubeChannelManager.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (User.ResolveOwnerOid(existing.CreatedByEntraOid, requireAdminWhenTargetingOtherUser: true) is null)
        {
            logger.LogWarning("User {CurrentOid} attempted to update YouTube channel config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(User.GetOwnerOid()), id, LogSanitizer.Sanitize(existing.CreatedByEntraOid));
            return Forbid();
        }

        if (!existing.HasApiKey && string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest("ApiKey is required when no API key has been previously stored.");
        }

        var config = mapper.Map<UserCollectorYouTubeChannel>(request);
        config.Id = id;
        config.CreatedByEntraOid = existing.CreatedByEntraOid;

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            await youTubeChannelManager.StoreApiKeyToKeyVaultAsync(
                existing.CreatedByEntraOid,
                request.ChannelId,
                request.ApiKey);
        }

        var saved = await youTubeChannelManager.SaveAsync(config);
        if (saved is null)
        {
            logger.LogWarning("Failed to update YouTube channel config for ID {Id}", id);
            return BadRequest("Unable to update YouTube channel configuration");
        }

        return Ok(mapper.Map<UserCollectorYouTubeChannelResponse>(saved));
    }

    /// <summary>
    /// Deletes a YouTube channel configuration.
    /// </summary>
    /// <param name="id">The configuration identifier.</param>
    /// <returns>No content when the delete succeeds.</returns>
    /// <response code="204">The YouTube channel configuration was deleted.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to delete this configuration.</response>
    /// <response code="404">No configuration exists with the specified ID.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        var config = await youTubeChannelManager.GetByIdAsync(id);
        if (config is null)
        {
            logger.LogWarning("YouTube channel config not found for delete for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();

        if (User.ResolveOwnerOid(config.CreatedByEntraOid, requireAdminWhenTargetingOtherUser: true) is null)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to delete YouTube channel config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(config.CreatedByEntraOid));
            return Forbid();
        }

        var deleted = await youTubeChannelManager.DeleteAsync(id, config.CreatedByEntraOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete YouTube channel config for ID {Id}", id);
            return NotFound();
        }

        return NoContent();
    }
}
