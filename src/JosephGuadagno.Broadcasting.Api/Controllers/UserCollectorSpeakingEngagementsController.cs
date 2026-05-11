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
/// Manages per-user speaking engagements file collector configurations
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class UserCollectorSpeakingEngagementsController(
    IUserCollectorSpeakingEngagementManager userCollectorSpeakingEngagementManager,
    ILogger<UserCollectorSpeakingEngagementsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all speaking engagement configurations visible to the current caller
    /// </summary>
    /// <param name="ownerOid">
    /// Optional Entra object ID to query. Non-admin callers can only query their own configurations.
    /// </param>
    /// <returns>A list of speaking engagement configurations for the resolved owner</returns>
    /// <response code="200">Returns the speaking engagement configurations for the resolved owner</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to query the requested owner</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<UserCollectorSpeakingEngagementResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<UserCollectorSpeakingEngagementResponse>>> GetAllAsync(
        [FromQuery] string? ownerOid = null,
        int page = Pagination.DefaultPage,
        int pageSize = Pagination.DefaultPageSize,
        string sortBy = "displayname",
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

        var result = await userCollectorSpeakingEngagementManager.GetAllAsync(resolvedOwnerOid, page, pageSize, sortBy, sortDescending, filter);
        var items = mapper.Map<List<UserCollectorSpeakingEngagementResponse>>(result.Items);
        return new PagedResponse<UserCollectorSpeakingEngagementResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Gets a speaking engagement configuration by ID
    /// </summary>
    /// <param name="id">The configuration identifier</param>
    /// <returns>The speaking engagement configuration</returns>
    /// <response code="200">Returns the speaking engagement configuration</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to access this configuration</response>
    /// <response code="404">No configuration exists with the specified ID</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorSpeakingEngagementResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCollectorSpeakingEngagementResponse>> GetAsync(int id)
    {
        var config = await userCollectorSpeakingEngagementManager.GetByIdAsync(id);
        if (config is null)
        {
            logger.LogWarning("Speaking engagement config not found for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();

        if (User.ResolveOwnerOid(config.CreatedByEntraOid, requireAdminWhenTargetingOtherUser: true) is null)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to access speaking engagement config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(config.CreatedByEntraOid));
            return Forbid();
        }

        return Ok(mapper.Map<UserCollectorSpeakingEngagementResponse>(config));
    }

    /// <summary>
    /// Creates a new speaking engagement configuration
    /// </summary>
    /// <param name="ownerOid">
    /// Optional Entra object ID to target. Non-admin callers can only save their own configurations.
    /// </param>
    /// <param name="request">The speaking engagement configuration payload to save</param>
    /// <returns>The saved speaking engagement configuration</returns>
    /// <response code="200">Returns the saved speaking engagement configuration</response>
    /// <response code="400">The request payload was invalid or the configuration could not be saved</response>
    /// <response code="401">The caller is not authenticated</response>
    /// <response code="403">The caller is not allowed to save configurations for the requested owner</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorSpeakingEngagementResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserCollectorSpeakingEngagementResponse>> SaveAsync(
        [FromQuery] string? ownerOid,
        [FromBody] UserCollectorSpeakingEngagementRequest request)
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

        var config = mapper.Map<UserCollectorSpeakingEngagement>(request);
        config.CreatedByEntraOid = resolvedOwnerOid;

        var saved = await userCollectorSpeakingEngagementManager.SaveAsync(config);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to save speaking engagement config for owner {OwnerOid} and file {File}",
                LogSanitizer.Sanitize(resolvedOwnerOid),
                LogSanitizer.Sanitize(request.SpeakingEngagementsFile));
            return BadRequest("Unable to save speaking engagement configuration");
        }

        return Ok(mapper.Map<UserCollectorSpeakingEngagementResponse>(saved));
    }

    /// <summary>
    /// Updates an existing speaking engagement configuration
    /// </summary>
    /// <param name="id">The configuration identifier</param>
    /// <param name="request">The speaking engagement configuration payload</param>
    /// <returns>The updated speaking engagement configuration</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorSpeakingEngagementResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCollectorSpeakingEngagementResponse>> UpdateAsync(
        int id,
        [FromBody] UserCollectorSpeakingEngagementRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("UpdateAsync called with invalid model state for ID {Id}", id);
            return BadRequest(ModelState);
        }

        var existing = await userCollectorSpeakingEngagementManager.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (User.ResolveOwnerOid(existing.CreatedByEntraOid, requireAdminWhenTargetingOtherUser: true) is null)
        {
            logger.LogWarning("User {CurrentOid} attempted to update speaking engagement config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(User.GetOwnerOid()), id, LogSanitizer.Sanitize(existing.CreatedByEntraOid));
            return Forbid();
        }

        var config = mapper.Map<UserCollectorSpeakingEngagement>(request);
        config.Id = id;
        config.CreatedByEntraOid = existing.CreatedByEntraOid;

        var saved = await userCollectorSpeakingEngagementManager.SaveAsync(config);
        if (saved is null)
        {
            logger.LogWarning("Failed to update speaking engagement config for ID {Id}", id);
            return BadRequest("Unable to update speaking engagement configuration");
        }

        return Ok(mapper.Map<UserCollectorSpeakingEngagementResponse>(saved));
    }

    /// <summary>
    /// Deletes a speaking engagement configuration
    /// </summary>
    /// <param name="id">The configuration identifier</param>
    /// <returns>No content when the delete succeeds</returns>
    /// <response code="204">The speaking engagement configuration was deleted</response>
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
        var config = await userCollectorSpeakingEngagementManager.GetByIdAsync(id);
        if (config is null)
        {
            logger.LogWarning("Speaking engagement config not found for delete for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();

        if (User.ResolveOwnerOid(config.CreatedByEntraOid, requireAdminWhenTargetingOtherUser: true) is null)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to delete speaking engagement config {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(config.CreatedByEntraOid));
            return Forbid();
        }

        var deleted = await userCollectorSpeakingEngagementManager.DeleteAsync(id, config.CreatedByEntraOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete speaking engagement config for ID {Id}", id);
            return NotFound();
        }

        return NoContent();
    }
}
