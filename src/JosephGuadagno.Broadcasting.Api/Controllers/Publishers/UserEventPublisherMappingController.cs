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
/// Manages per-user event-to-publisher mappings.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Publishers/EventPublisherMappings")]
[Produces("application/json")]
public class UserEventPublisherMappingController(
    IUserEventPublisherMappingManager manager,
    IOnboardingManager onboardingManager,
    ILogger<UserEventPublisherMappingController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all event publisher mappings for the authenticated user.
    /// </summary>
    /// <returns>The authenticated user's event publisher mappings.</returns>
    /// <response code="200">Returns the authenticated user's event publisher mappings. The list may be empty.</response>
    /// <response code="401">The caller is not authenticated.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserEventPublisherMappingResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<UserEventPublisherMappingResponse>>> GetAllAsync()
    {
        var ownerOid = User.GetOwnerOid();
        var mappings = await manager.GetByUserAsync(ownerOid);
        return Ok(mapper.Map<List<UserEventPublisherMappingResponse>>(mappings));
    }

    /// <summary>
    /// Gets an event publisher mapping by identifier.
    /// </summary>
    /// <param name="id">The event publisher mapping identifier.</param>
    /// <returns>The requested event publisher mapping.</returns>
    /// <response code="200">Returns the requested event publisher mapping.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to access this event publisher mapping.</response>
    /// <response code="404">No event publisher mapping exists with the specified identifier.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserEventPublisherMappingResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserEventPublisherMappingResponse>> GetAsync(int id)
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
                "User {CurrentOid} attempted to access event publisher mapping {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(mapping.CreatedByEntraOid));
            return Forbid();
        }

        return Ok(mapper.Map<UserEventPublisherMappingResponse>(mapping));
    }

    /// <summary>
    /// Creates an event publisher mapping for the authenticated user.
    /// </summary>
    /// <param name="request">The event publisher mapping payload to create.</param>
    /// <returns>The created event publisher mapping.</returns>
    /// <response code="201">Returns the created event publisher mapping.</response>
    /// <response code="400">The request payload was invalid or the event publisher mapping could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserEventPublisherMappingResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserEventPublisherMappingResponse>> CreateAsync([FromBody] CreateUserEventPublisherMappingRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("CreateAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var ownerOid = User.GetOwnerOid();
        var mapping = mapper.Map<UserEventPublisherMapping>(request);
        mapping.CreatedByEntraOid = ownerOid;

        var saved = await manager.SaveAsync(mapping);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to save event publisher mapping for owner {OwnerOid}, event type {EventType}, platform {PlatformId}",
                LogSanitizer.Sanitize(ownerOid),
                LogSanitizer.Sanitize(request.EventType),
                request.SocialMediaPlatformId);
            return BadRequest("Unable to save event publisher mapping");
        }

        await onboardingManager.RecalculateAsync(ownerOid);
        return CreatedAtAction(nameof(GetAsync), new { id = saved.Id }, mapper.Map<UserEventPublisherMappingResponse>(saved));
    }

    /// <summary>
    /// Updates an existing event publisher mapping.
    /// </summary>
    /// <param name="id">The event publisher mapping identifier.</param>
    /// <param name="request">The event publisher mapping payload. Null properties keep their existing values.</param>
    /// <returns>The updated event publisher mapping.</returns>
    /// <response code="200">Returns the updated event publisher mapping.</response>
    /// <response code="400">The request payload was invalid or the event publisher mapping could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to update this event publisher mapping.</response>
    /// <response code="404">No event publisher mapping exists with the specified identifier.</response>
    [HttpPut("{id:int}")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserEventPublisherMappingResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserEventPublisherMappingResponse>> UpdateAsync(int id, [FromBody] UpdateUserEventPublisherMappingRequest request)
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
                "User {CurrentOid} attempted to update event publisher mapping {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(existing.CreatedByEntraOid));
            return Forbid();
        }

        mapper.Map(request, existing);

        var saved = await manager.SaveAsync(existing);
        if (saved is null)
        {
            logger.LogWarning("Failed to update event publisher mapping for ID {Id}", id);
            return BadRequest("Unable to update event publisher mapping");
        }

        await onboardingManager.RecalculateAsync(existing.CreatedByEntraOid);
        return Ok(mapper.Map<UserEventPublisherMappingResponse>(saved));
    }

    /// <summary>
    /// Deletes an event publisher mapping.
    /// </summary>
    /// <param name="id">The event publisher mapping identifier.</param>
    /// <returns>No content when the delete succeeds.</returns>
    /// <response code="204">The event publisher mapping was deleted.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to delete this event publisher mapping.</response>
    /// <response code="404">No event publisher mapping exists with the specified identifier.</response>
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
                "User {CurrentOid} attempted to delete event publisher mapping {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(mapping.CreatedByEntraOid));
            return Forbid();
        }

        var deleted = await manager.DeleteAsync(id, mapping.CreatedByEntraOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete event publisher mapping for ID {Id}", id);
            return NotFound();
        }

        await onboardingManager.RecalculateAsync(mapping.CreatedByEntraOid);
        return NoContent();
    }
}
