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
    IUserCollectorSpeakingEngagementManager speakingEngagementManager,
    IUserCollectorScheduledItemManager scheduledItemManager) : ControllerBase
{
    /// <summary>
    /// Gets a summary of all collector configurations (YouTube channels, feed sources, speaking engagements,
    /// and scheduled item) for the resolved owner in a single call.
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

        var youTubeTask = youTubeChannelManager.GetByUserAsync(resolvedOwnerOid);
        var feedTask = feedSourceManager.GetByUserAsync(resolvedOwnerOid);
        var speakingTask = speakingEngagementManager.GetByUserAsync(resolvedOwnerOid);
        var scheduledTask = scheduledItemManager.GetByUserAsync(resolvedOwnerOid);

        await Task.WhenAll(youTubeTask, feedTask, speakingTask, scheduledTask);

        return Ok(new CollectorsSummaryResponse
        {
            YouTubeChannelCount = youTubeTask.Result.Count,
            FeedSourceCount = feedTask.Result.Count,
            SpeakingEngagementCount = speakingTask.Result.Count,
            ScheduledItemConfigured = scheduledTask.Result.Count > 0
        });
    }
}
