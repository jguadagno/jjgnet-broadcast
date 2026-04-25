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
/// Manages per-user YouTube channel collector configurations
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class UserCollectorYouTubeChannelsController(
    IUserCollectorYouTubeChannelManager userCollectorYouTubeChannelManager,
    ILogger<UserCollectorYouTubeChannelsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all YouTube channel configurations visible to the current caller
    /// </summary>
    /// <param name="ownerOid">
    /// Optional Entra object ID to query. Non-admin callers can only query their own configurations.
    /// </param>
    /// <returns>A list of YouTube channel configurations for the resolved owner</returns>
    /// <response code="200">Returns the YouTube channel configurations for the resolved owner</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to query the requested owner</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserCollectorYouTubeChannelResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserCollectorYouTubeChannelResponse>>> GetAllAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var configs = await userCollectorYouTubeChannelManager.GetByUserAsync(resolvedOwnerOid);
        return Ok(mapper.Map<List<UserCollectorYouTubeChannelResponse>>(configs));
    }

    /// <summary>
    /// Gets a YouTube channel configuration by ID
    /// </summary>
    /// <param name="id">The configuration identifier</param>
    /// <returns>The YouTube channel configuration</returns>
    /// <response code="200">Returns the YouTube channel configuration</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to access this configuration</response>
    /// <response code="404">No configuration exists with the specified ID</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorYouTubeChannelResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCollectorYouTubeChannelResponse>> GetAsync(int id)
    {
        var config = await userCollectorYouTubeChannelManager.GetByIdAsync(id);
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
    /// Creates or updates a YouTube channel configuration
    /// </summary>
    /// <param name="ownerOid">
    /// Optional Entra object ID to target. Non-admin callers can only save their own configurations.
    /// </param>
    /// <param name="request">The YouTube channel configuration payload to save</param>
    /// <returns>The saved YouTube channel configuration</returns>
    /// <response code="200">Returns the saved YouTube channel configuration</response>
    /// <response code="400">The request payload was invalid or the configuration could not be saved</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to save configurations for the requested owner</response>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorYouTubeChannelResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserCollectorYouTubeChannelResponse>> SaveAsync(
        [FromQuery] string? ownerOid,
        [FromBody] UserCollectorYouTubeChannelRequest request)
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

        var saved = await userCollectorYouTubeChannelManager.SaveAsync(config);
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
    /// Deletes a YouTube channel configuration
    /// </summary>
    /// <param name="id">The configuration identifier</param>
    /// <returns>No content when the delete succeeds</returns>
    /// <response code="204">The YouTube channel configuration was deleted</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to delete this configuration</response>
    /// <response code="404">No configuration exists with the specified ID</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        var config = await userCollectorYouTubeChannelManager.GetByIdAsync(id);
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

        var deleted = await userCollectorYouTubeChannelManager.DeleteAsync(id, config.CreatedByEntraOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete YouTube channel config for ID {Id}", id);
            return NotFound();
        }

        return NoContent();
    }

}
