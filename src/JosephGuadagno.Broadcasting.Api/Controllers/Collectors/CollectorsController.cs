using JosephGuadagno.Broadcasting.Api;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers.Collectors;

/// <summary>
/// Returns an aggregate summary of all collector configurations for the resolved owner.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("Collectors")]
[Produces("application/json")]
public class CollectorsController(
    IUserCollectorYouTubeChannelManager youTubeChannelManager,
    IUserCollectorFeedSourceManager feedSourceManager,
    IUserCollectorSpeakingEngagementManager speakingEngagementManager) : ControllerBase
{
    /// <summary>
    /// Gets a summary of all collector configurations (YouTube channels, feed sources, and speaking engagements)
    /// for the resolved owner in a single call.
    /// </summary>
    /// <param name="ownerOid">Optional Entra OID. Non-admin callers can only query their own settings.</param>
    /// <returns>A summary of collector configuration counts and configured status for the resolved owner.</returns>
    /// <response code="200">Returns the collector summary for the resolved owner.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller is not allowed to query the requested owner.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CollectorsSummaryResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CollectorsSummaryResponse>> GetAllAsync([FromQuery] string? ownerOid = null)
    {
        var resolvedOwnerOid = User.ResolveOwnerOid(ownerOid, requireAdminWhenTargetingOtherUser: true);
        if (resolvedOwnerOid is null)
        {
            return Forbid();
        }

        var youTube = await youTubeChannelManager.GetByUserAsync(resolvedOwnerOid);
        var feed = await feedSourceManager.GetByUserAsync(resolvedOwnerOid);
        var speaking = await speakingEngagementManager.GetByUserAsync(resolvedOwnerOid);

        return Ok(new CollectorsSummaryResponse
        {
            YouTubeChannelCount = youTube.Count,
            FeedSourceCount = feed.Count,
            SpeakingEngagementCount = speaking.Count,
        });
    }
}
