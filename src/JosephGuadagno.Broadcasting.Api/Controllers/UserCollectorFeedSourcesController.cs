using System.Security.Claims;
using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Manages per-user RSS/Atom/JSON feed collector configurations
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class UserCollectorFeedSourcesController(
    IUserCollectorFeedSourceManager userCollectorFeedSourceManager,
    ILogger<UserCollectorFeedSourcesController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all feed source configurations visible to the current caller
    /// </summary>
    /// <param name="ownerOid">
    /// Optional Entra object ID to query. Non-admin callers can only query their own configurations.
    /// </param>
    /// <returns>A list of feed source configurations for the resolved owner</returns>
    /// <response code="200">Returns the feed source configurations for the resolved owner</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to query the requested owner</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserCollectorFeedSourceResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserCollectorFeedSourceResponse>>> GetAllAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var configs = await userCollectorFeedSourceManager.GetByUserAsync(resolvedOwnerOid);
        return Ok(mapper.Map<List<UserCollectorFeedSourceResponse>>(configs));
    }

    /// <summary>
    /// Gets a feed source configuration by ID
    /// </summary>
    /// <param name="id">The configuration identifier</param>
    /// <returns>The feed source configuration</returns>
    /// <response code="200">Returns the feed source configuration</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to access this configuration</response>
    /// <response code="404">No configuration exists with the specified ID</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorFeedSourceResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCollectorFeedSourceResponse>> GetAsync(int id)
    {
        var config = await userCollectorFeedSourceManager.GetByIdAsync(id);
        if (config is null)
        {
            logger.LogWarning("Feed source config not found for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId)
            ?? throw new InvalidOperationException("Entra Object ID claim not found");

        if (!string.Equals(config.CreatedByEntraOid, currentOwnerOid, StringComparison.OrdinalIgnoreCase)
            && !User.IsInRole(RoleNames.SiteAdministrator))
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to access feed source config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(config.CreatedByEntraOid));
            return Forbid();
        }

        return Ok(mapper.Map<UserCollectorFeedSourceResponse>(config));
    }

    /// <summary>
    /// Creates or updates a feed source configuration
    /// </summary>
    /// <param name="ownerOid">
    /// Optional Entra object ID to target. Non-admin callers can only save their own configurations.
    /// </param>
    /// <param name="request">The feed source configuration payload to save</param>
    /// <returns>The saved feed source configuration</returns>
    /// <response code="200">Returns the saved feed source configuration</response>
    /// <response code="400">The request payload was invalid or the configuration could not be saved</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to save configurations for the requested owner</response>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorFeedSourceResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserCollectorFeedSourceResponse>> SaveAsync(
        [FromQuery] string? ownerOid,
        [FromBody] UserCollectorFeedSourceRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("SaveAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var resolvedOwnerOid = ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var config = mapper.Map<UserCollectorFeedSource>(request);
        config.CreatedByEntraOid = resolvedOwnerOid;

        var saved = await userCollectorFeedSourceManager.SaveAsync(config);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to save feed source config for owner {OwnerOid} and feed URL {FeedUrl}",
                LogSanitizer.Sanitize(resolvedOwnerOid),
                LogSanitizer.Sanitize(request.FeedUrl));
            return BadRequest("Unable to save feed source configuration");
        }

        return Ok(mapper.Map<UserCollectorFeedSourceResponse>(saved));
    }

    /// <summary>
    /// Deletes a feed source configuration
    /// </summary>
    /// <param name="id">The configuration identifier</param>
    /// <returns>No content when the delete succeeds</returns>
    /// <response code="204">The feed source configuration was deleted</response>
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
        var config = await userCollectorFeedSourceManager.GetByIdAsync(id);
        if (config is null)
        {
            logger.LogWarning("Feed source config not found for delete for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId)
            ?? throw new InvalidOperationException("Entra Object ID claim not found");

        if (!string.Equals(config.CreatedByEntraOid, currentOwnerOid, StringComparison.OrdinalIgnoreCase)
            && !User.IsInRole(RoleNames.SiteAdministrator))
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to delete feed source config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(config.CreatedByEntraOid));
            return Forbid();
        }

        var deleted = await userCollectorFeedSourceManager.DeleteAsync(id, config.CreatedByEntraOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete feed source config for ID {Id}", id);
            return NotFound();
        }

        return NoContent();
    }

    private string? ResolveOwnerOid(string? requestedOwnerOid, bool requireAdminWhenTargetingOtherUser)
    {
        var currentOwnerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId)
                              ?? throw new InvalidOperationException("Entra Object ID claim not found");

        if (string.IsNullOrWhiteSpace(requestedOwnerOid)
            || string.Equals(requestedOwnerOid, currentOwnerOid, StringComparison.OrdinalIgnoreCase))
        {
            return currentOwnerOid;
        }

        return requireAdminWhenTargetingOtherUser && !User.IsInRole(RoleNames.SiteAdministrator)
            ? null
            : requestedOwnerOid;
    }
}
