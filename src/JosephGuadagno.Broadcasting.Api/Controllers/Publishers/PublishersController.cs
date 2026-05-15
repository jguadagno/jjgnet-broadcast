using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers.Publishers;

/// <summary>
/// Returns an aggregate view of all publisher settings for the resolved owner.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class PublishersController(
    IUserPublisherBlueskySettingsManager blueskyManager,
    IUserPublisherTwitterSettingsManager twitterManager,
    IUserPublisherLinkedInSettingsManager linkedInManager,
    IUserPublisherFacebookSettingsManager facebookManager,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Gets all publisher settings (Bluesky, Twitter, LinkedIn, Facebook) for the resolved owner in one call.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own settings.</param>
    /// <response code="200">Returns the aggregate publisher settings (null for unconfigured platforms).</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller may not query the requested owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PublishersAggregateResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PublishersAggregateResponse>> GetAllAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var blueskyTask = blueskyManager.GetAsync(resolvedOwnerOid);
        var twitterTask = twitterManager.GetAsync(resolvedOwnerOid);
        var linkedInTask = linkedInManager.GetAsync(resolvedOwnerOid);
        var facebookTask = facebookManager.GetAsync(resolvedOwnerOid);

        await Task.WhenAll(blueskyTask, twitterTask, linkedInTask, facebookTask);

        return Ok(new PublishersAggregateResponse
        {
            Bluesky = blueskyTask.Result is null ? null : mapper.Map<BlueskySettingsResponse>(blueskyTask.Result),
            Twitter = twitterTask.Result is null ? null : mapper.Map<TwitterSettingsResponse>(twitterTask.Result),
            LinkedIn = linkedInTask.Result is null ? null : mapper.Map<LinkedInSettingsResponse>(linkedInTask.Result),
            Facebook = facebookTask.Result is null ? null : mapper.Map<FacebookSettingsResponse>(facebookTask.Result)
        });
    }
}
