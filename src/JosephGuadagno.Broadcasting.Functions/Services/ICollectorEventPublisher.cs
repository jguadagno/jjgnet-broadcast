using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Functions.Services;

public interface ICollectorEventPublisher
{
    Task PublishSyndicationFeedItemAsync(SyndicationFeedItem item, string ownerOid, CancellationToken cancellationToken = default);
    Task PublishYouTubeItemAsync(YouTubeItem item, string ownerOid, CancellationToken cancellationToken = default);
    Task PublishSpeakingEngagementAsync(Engagement item, string ownerOid, CancellationToken cancellationToken = default);
}
