using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Functions.Services;

public interface ICollectorEventDispatcher
{
    Task DispatchSyndicationFeedItemAsync(SyndicationFeedItem item, string ownerOid, CancellationToken cancellationToken = default);
    Task DispatchYouTubeItemAsync(YouTubeItem item, string ownerOid, CancellationToken cancellationToken = default);
    Task DispatchSpeakingEngagementAsync(Engagement item, string ownerOid, CancellationToken cancellationToken = default);
}
