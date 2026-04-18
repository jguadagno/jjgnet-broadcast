using System.Security.Claims;
using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

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
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserPublisherSettingResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserPublisherSettingResponse>>> GetAllAsync([FromQuery] string? ownerOid = null)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.UserPublisherSettings.List, Domain.Scopes.UserPublisherSettings.All);

        var resolvedOwnerOid = ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var settings = await userPublisherSettingManager.GetByUserAsync(resolvedOwnerOid);
        return Ok(mapper.Map<List<UserPublisherSettingResponse>>(settings));
    }

    [HttpGet("{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserPublisherSettingResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPublisherSettingResponse>> GetAsync(int platformId, [FromQuery] string? ownerOid = null)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.UserPublisherSettings.View, Domain.Scopes.UserPublisherSettings.All);

        var resolvedOwnerOid = ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var setting = await userPublisherSettingManager.GetByUserAndPlatformAsync(resolvedOwnerOid, platformId);
        if (setting is null)
        {
            logger.LogWarning("User publisher settings not found for owner {OwnerOid} and platform {PlatformId}", resolvedOwnerOid, platformId);
            return NotFound();
        }

        return Ok(mapper.Map<UserPublisherSettingResponse>(setting));
    }

    [HttpPut("{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserPublisherSettingResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPublisherSettingResponse>> SaveAsync(
        int platformId,
        [FromQuery] string? ownerOid,
        [FromBody] UserPublisherSettingRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.UserPublisherSettings.Modify, Domain.Scopes.UserPublisherSettings.All);

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

        var setting = mapper.Map<UserPublisherSettingUpdate>(request);
        setting.CreatedByEntraOid = resolvedOwnerOid;
        setting.SocialMediaPlatformId = platformId;

        var saved = await userPublisherSettingManager.SaveAsync(setting);
        if (saved is null)
        {
            logger.LogWarning("Failed to save user publisher settings for owner {OwnerOid} and platform {PlatformId}", resolvedOwnerOid, platformId);
            return BadRequest("Unable to save publisher settings");
        }

        return Ok(mapper.Map<UserPublisherSettingResponse>(saved));
    }

    [HttpDelete("{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(int platformId, [FromQuery] string? ownerOid = null)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.UserPublisherSettings.Delete, Domain.Scopes.UserPublisherSettings.All);

        var resolvedOwnerOid = ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var deleted = await userPublisherSettingManager.DeleteAsync(resolvedOwnerOid, platformId);
        if (!deleted)
        {
            logger.LogWarning("User publisher settings not found for delete for owner {OwnerOid} and platform {PlatformId}", resolvedOwnerOid, platformId);
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
