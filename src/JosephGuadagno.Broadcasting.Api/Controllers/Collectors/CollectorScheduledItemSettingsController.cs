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
/// Manages the per-user scheduled item collector configuration under the
/// <c>/Collectors/ScheduledItem/Settings</c> route. Each user has at most one
/// scheduled item configuration (enforced by a UNIQUE constraint on <c>CreatedByEntraOId</c>).
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Collectors/ScheduledItem/Settings")]
[Produces("application/json")]
public class CollectorScheduledItemSettingsController(
    IUserCollectorScheduledItemManager scheduledItemManager,
    ILogger<CollectorScheduledItemSettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets the scheduled item collector configuration for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own configuration.</param>
    /// <returns>The scheduled item configuration for the resolved owner.</returns>
    /// <response code="200">Returns the scheduled item configuration.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to query the requested owner.</response>
    /// <response code="404">No scheduled item configuration exists for the resolved owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCollectorScheduledItemResponse>> GetAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var configs = await scheduledItemManager.GetByUserAsync(resolvedOwnerOid);
        var config = configs.FirstOrDefault();
        if (config is null)
        {
            logger.LogWarning("Scheduled item config not found for owner {OwnerOid}", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NotFound();
        }

        return Ok(mapper.Map<UserCollectorScheduledItemResponse>(config));
    }

    /// <summary>
    /// Creates or updates the scheduled item collector configuration for the resolved owner.
    /// If a configuration already exists it is updated; otherwise a new one is created.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only upsert their own configuration.</param>
    /// <param name="request">The scheduled item configuration payload.</param>
    /// <returns>The saved scheduled item configuration.</returns>
    /// <response code="200">Returns the saved scheduled item configuration.</response>
    /// <response code="400">The request payload was invalid or the configuration could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to upsert the requested owner's configuration.</response>
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCollectorScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserCollectorScheduledItemResponse>> UpsertAsync(
        [FromQuery] string? ownerOid,
        [FromBody] UserCollectorScheduledItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("UpsertAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var existing = (await scheduledItemManager.GetByUserAsync(resolvedOwnerOid)).FirstOrDefault();

        var config = mapper.Map<UserCollectorScheduledItem>(request);
        config.CreatedByEntraOid = resolvedOwnerOid;

        if (existing is not null)
        {
            config.Id = existing.Id;
        }

        var saved = await scheduledItemManager.SaveAsync(config);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to upsert scheduled item config for owner {OwnerOid}",
                LogSanitizer.Sanitize(resolvedOwnerOid));
            return BadRequest("Unable to save scheduled item configuration");
        }

        return Ok(mapper.Map<UserCollectorScheduledItemResponse>(saved));
    }

    /// <summary>
    /// Deletes the scheduled item collector configuration for the resolved owner.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only delete their own configuration.</param>
    /// <returns>No content when the delete succeeds.</returns>
    /// <response code="204">The scheduled item configuration was deleted.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to delete the requested owner's configuration.</response>
    /// <response code="404">No scheduled item configuration exists for the resolved owner.</response>
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

        var existing = (await scheduledItemManager.GetByUserAsync(resolvedOwnerOid)).FirstOrDefault();
        if (existing is null)
        {
            logger.LogWarning("Scheduled item config not found for delete for owner {OwnerOid}", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NotFound();
        }

        var deleted = await scheduledItemManager.DeleteAsync(existing.Id, resolvedOwnerOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete scheduled item config for owner {OwnerOid}", LogSanitizer.Sanitize(resolvedOwnerOid));
            return NotFound();
        }

        return NoContent();
    }
}
