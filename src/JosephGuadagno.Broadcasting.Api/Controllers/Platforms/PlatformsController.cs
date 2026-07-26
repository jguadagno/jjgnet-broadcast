using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers.Platforms;

/// <summary>
/// Returns an aggregate view of all platform settings for the resolved owner.
/// </summary>
[ApiController]
[Tags("Platforms")]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Platforms")]
[Produces("application/json")]
public class PlatformsController(
    IUserPlatformBlueskySettingsManager blueskyManager,
    IUserPlatformTwitterSettingsManager twitterManager,
    IUserPlatformLinkedInSettingsManager linkedInManager,
    IUserPlatformFacebookSettingsManager facebookManager,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all platform settings (Bluesky, Twitter, LinkedIn, Facebook) for the resolved owner in one call.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own settings.</param>
    /// <response code="200">Returns the aggregate platform settings (null for unconfigured platforms).</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller may not query the requested owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlatformsAggregateResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlatformsAggregateResponse>> GetAllAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var bluesky = await blueskyManager.GetAsync(resolvedOwnerOid);
        var twitter = await twitterManager.GetAsync(resolvedOwnerOid);
        var linkedIn = await linkedInManager.GetAsync(resolvedOwnerOid);
        var facebook = await facebookManager.GetAsync(resolvedOwnerOid);

        return Ok(new PlatformsAggregateResponse
        {
            Bluesky = bluesky is null ? null : mapper.Map<BlueskySettingsResponse>(bluesky),
            Twitter = twitter is null ? null : mapper.Map<TwitterSettingsResponse>(twitter),
            LinkedIn = linkedIn is null ? null : mapper.Map<LinkedInSettingsResponse>(linkedIn),
            Facebook = facebook is null ? null : mapper.Map<FacebookSettingsResponse>(facebook)
        });
    }
}

