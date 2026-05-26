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
/// Manages per-user random post schedules and filtering settings.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Publishers/RandomPostSettings")]
[Produces("application/json")]
public class UserRandomPostSettingsController(
    IUserRandomPostSettingsManager manager,
    IOnboardingManager onboardingManager,
    ILogger<UserRandomPostSettingsController> logger,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all random post settings for the authenticated user.
    /// </summary>
    /// <returns>The authenticated user's random post settings.</returns>
    /// <response code="200">Returns the authenticated user's random post settings. The list may be empty.</response>
    /// <response code="401">The caller is not authenticated.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserRandomPostSettingsResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<UserRandomPostSettingsResponse>>> GetAllAsync()
    {
        var ownerOid = User.GetOwnerOid();
        var settings = await manager.GetByUserAsync(ownerOid);
        return Ok(mapper.Map<List<UserRandomPostSettingsResponse>>(settings));
    }

    /// <summary>
    /// Gets a random post settings record by identifier.
    /// </summary>
    /// <param name="id">The random post settings identifier.</param>
    /// <returns>The requested random post settings record.</returns>
    /// <response code="200">Returns the requested random post settings record.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to access this random post settings record.</response>
    /// <response code="404">No random post settings record exists with the specified identifier.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserRandomPostSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserRandomPostSettingsResponse>> GetAsync(int id)
    {
        var settings = await manager.GetByIdAsync(id);
        if (settings is null)
        {
            logger.LogWarning("Random post settings not found for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();
        if (!User.IsSiteAdministrator() && settings.CreatedByEntraOid != currentOwnerOid)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to access random post settings {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
            return Forbid();
        }

        return Ok(mapper.Map<UserRandomPostSettingsResponse>(settings));
    }

    /// <summary>
    /// Creates a random post settings record for the authenticated user.
    /// </summary>
    /// <param name="request">The random post settings payload to create.</param>
    /// <returns>The created random post settings record.</returns>
    /// <response code="201">Returns the created random post settings record.</response>
    /// <response code="400">The request payload was invalid or the random post settings record could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserRandomPostSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserRandomPostSettingsResponse>> CreateAsync([FromBody] CreateUserRandomPostSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("CreateAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var ownerOid = User.GetOwnerOid();
        var settings = mapper.Map<UserRandomPostSettings>(request);
        settings.CreatedByEntraOid = ownerOid;

        var saved = await manager.SaveAsync(settings);
        if (saved is null)
        {
            logger.LogWarning(
                "Failed to save random post settings for owner {OwnerOid}, platform {PlatformId}, cron {CronExpression}",
                LogSanitizer.Sanitize(ownerOid),
                request.SocialMediaPlatformId,
                LogSanitizer.Sanitize(request.CronExpression));
            return BadRequest("Unable to save random post settings");
        }

        await onboardingManager.RecalculateAsync(ownerOid);
        return CreatedAtAction(nameof(GetAsync), new { id = saved.Id }, mapper.Map<UserRandomPostSettingsResponse>(saved));
    }

    /// <summary>
    /// Updates an existing random post settings record.
    /// </summary>
    /// <param name="id">The random post settings identifier.</param>
    /// <param name="request">The random post settings payload. Null properties keep their existing values.</param>
    /// <returns>The updated random post settings record.</returns>
    /// <response code="200">Returns the updated random post settings record.</response>
    /// <response code="400">The request payload was invalid or the random post settings record could not be saved.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to update this random post settings record.</response>
    /// <response code="404">No random post settings record exists with the specified identifier.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserRandomPostSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserRandomPostSettingsResponse>> UpdateAsync(int id, [FromBody] UpdateUserRandomPostSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("UpdateAsync called with invalid model state for ID {Id}", id);
            return BadRequest(ModelState);
        }

        var existing = await manager.GetByIdAsync(id);
        if (existing is null)
        {
            logger.LogWarning("Random post settings not found for update for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();
        if (!User.IsSiteAdministrator() && existing.CreatedByEntraOid != currentOwnerOid)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to update random post settings {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(existing.CreatedByEntraOid));
            return Forbid();
        }

        mapper.Map(request, existing);

        var saved = await manager.SaveAsync(existing);
        if (saved is null)
        {
            logger.LogWarning("Failed to update random post settings for ID {Id}", id);
            return BadRequest("Unable to update random post settings");
        }

        await onboardingManager.RecalculateAsync(existing.CreatedByEntraOid);
        return Ok(mapper.Map<UserRandomPostSettingsResponse>(saved));
    }

    /// <summary>
    /// Deletes a random post settings record.
    /// </summary>
    /// <param name="id">The random post settings identifier.</param>
    /// <returns>No content when the delete succeeds.</returns>
    /// <response code="204">The random post settings record was deleted.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to delete this random post settings record.</response>
    /// <response code="404">No random post settings record exists with the specified identifier.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        var settings = await manager.GetByIdAsync(id);
        if (settings is null)
        {
            logger.LogWarning("Random post settings not found for delete for ID {Id}", id);
            return NotFound();
        }

        var currentOwnerOid = User.GetOwnerOid();
        if (!User.IsSiteAdministrator() && settings.CreatedByEntraOid != currentOwnerOid)
        {
            logger.LogWarning(
                "User {CurrentOid} attempted to delete random post settings {Id} owned by {OwnerOid}",
                LogSanitizer.Sanitize(currentOwnerOid),
                id,
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
            return Forbid();
        }

        var deleted = await manager.DeleteAsync(id, settings.CreatedByEntraOid);
        if (!deleted)
        {
            logger.LogWarning("Failed to delete random post settings for ID {Id}", id);
            return NotFound();
        }

        await onboardingManager.RecalculateAsync(settings.CreatedByEntraOid);
        return NoContent();
    }
}
