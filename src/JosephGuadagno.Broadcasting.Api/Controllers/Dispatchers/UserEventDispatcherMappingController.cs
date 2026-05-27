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
/// Manages per-user event-to-dispatcher mappings.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Dispatchers/EventDispatcherMappings")]
[Produces("application/json")]
public class UserEventDispatcherMappingController(
    IUserEventDispatcherMappingManager manager,
    IOnboardingManager onboardingManager,
    ILogger<UserEventDispatcherMappingController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all event dispatcher mappings for the authenticated user.
    /// </summary>
    /// <returns>The authenticated user's event dispatcher mappings.</returns>
    /// <response code="200">Returns the authenticated user's event dispatcher mappings. The list may be empty.</response>
    /// <response code="401">The caller is not authenticated.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserEventDispatcherMappingResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<UserEventDispatcherMappingResponse>>> GetAllAsync()
    {
        var ownerOid = User.GetOwnerOid();
        var mappings = await manager.GetByUserAsync(ownerOid);
        return Ok(mapper.Map<List<UserEventDispatcherMappingResponse>>(mappings));
    }

    /// <summary>
    /// Gets an event dispatcher mapping by identifier.
    /// </summary>
    /// <param name="id">The event dispatcher mapping identifier.</param>
    /// <returns>The requested event dispatcher mapping.</returns>
    /// <response code="200">Returns the requested event dispatcher mapping.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to access this event dispatcher mapping.</response>
    /// <response code="404">No event dispatcher mapping exists with the specified identifier.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserEventDispatcherMappingResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserEventDispatcherMappingResponse>> GetAsync(int id)
    {
        var mapping = await manager.GetByIdAsync(id);
        if (mapping is null)
        {
            logger.LogWarning("Event publisher mapping not found for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();
        if (!User.IsSiteAdministrator() && mapping.CreatedByEntraOid != currentOwnerOid)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to access event dispatcher mapping {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(mapping.CreatedByEntraOid));
            return Forbid();
        }

        return Ok(mapper.Map<UserEventDispatcherMappingResponse>(mapping));
    }

    /// <summary>
    /// Creates an event dispatcher mapping for the authenticated user.
    /// </summary>
    /// <param name="request">The event dispatcher mapping payload to create.</param>
    /// <returns>The created event dispatcher mapping.</returns>
    /// <response code="201">Returns the created event dispatcher mapping.</response>
    /// <response code="400">The request payload was invalid or the event dispatcher mapping could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserEventDispatcherMappingResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserEventDispatcherMappingResponse>> CreateAsync([FromBody] CreateUserEventDispatcherMappingRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("CreateAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var ownerOid = User.GetOwnerOid();
        var mapping = mapper.Map<UserEventDispatcherMapping>(request);
        mapping.CreatedByEntraOid = ownerOid;

        var saved = await manager.SaveAsync(mapping);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to save event dispatcher mapping for owner {OwnerOid}, event type {EventType}, platform {PlatformId}",
                LogSanitizer.Sanitize(ownerOid),
                LogSanitizer.Sanitize(request.EventType),
                request.SocialMediaPlatformId);
            return BadRequest("Unable to save event dispatcher mapping");
        }

        await onboardingManager.RecalculateAsync(ownerOid);
        return CreatedAtAction(nameof(GetAsync), new { id = saved.Id }, mapper.Map<UserEventDispatcherMappingResponse>(saved));
    }

    /// <summary>
    /// Updates an existing event dispatcher mapping.
    /// </summary>
    /// <param name="id">The event dispatcher mapping identifier.</param>
    /// <param name="request">The event dispatcher mapping payload. Null properties keep their existing values.</param>
    /// <returns>The updated event dispatcher mapping.</returns>
    /// <response code="200">Returns the updated event dispatcher mapping.</response>
    /// <response code="400">The request payload was invalid or the event dispatcher mapping could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to update this event dispatcher mapping.</response>
    /// <response code="404">No event dispatcher mapping exists with the specified identifier.</response>
    [HttpPut("{id:int}")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserEventDispatcherMappingResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserEventDispatcherMappingResponse>> UpdateAsync(int id, [FromBody] UpdateUserEventDispatcherMappingRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("UpdateAsync called with invalid model state for ID {Id}", id);
            return BadRequest(ModelState);
        }

        var existing = await manager.GetByIdAsync(id);
        if (existing is null)
        {
            logger.LogWarning("Event publisher mapping not found for update for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();
        if (!User.IsSiteAdministrator() && existing.CreatedByEntraOid != currentOwnerOid)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to update event dispatcher mapping {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(existing.CreatedByEntraOid));
            return Forbid();
        }

        mapper.Map(request, existing);

        var saved = await manager.SaveAsync(existing);
        if (saved is null)
        {
            logger.LogWarning("Failed to update event dispatcher mapping for ID {Id}", id);
            return BadRequest("Unable to update event dispatcher mapping");
        }

        await onboardingManager.RecalculateAsync(existing.CreatedByEntraOid);
        return Ok(mapper.Map<UserEventDispatcherMappingResponse>(saved));
    }

    /// <summary>
    /// Deletes an event dispatcher mapping.
    /// </summary>
    /// <param name="id">The event dispatcher mapping identifier.</param>
    /// <returns>No content when the delete succeeds.</returns>
    /// <response code="204">The event dispatcher mapping was deleted.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to delete this event dispatcher mapping.</response>
    /// <response code="404">No event dispatcher mapping exists with the specified identifier.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        var mapping = await manager.GetByIdAsync(id);
        if (mapping is null)
        {
            logger.LogWarning("Event publisher mapping not found for delete for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();
        if (!User.IsSiteAdministrator() && mapping.CreatedByEntraOid != currentOwnerOid)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to delete event dispatcher mapping {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(mapping.CreatedByEntraOid));
            return Forbid();
        }

        var deleted = await manager.DeleteAsync(id, mapping.CreatedByEntraOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete event dispatcher mapping for ID {Id}", id);
            return NotFound();
        }

        await onboardingManager.RecalculateAsync(mapping.CreatedByEntraOid);
        return NoContent();
    }
}
