using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Publishes domain events to Event Grid topics.
/// All methods complete successfully or throw <see cref="Exceptions.EventPublishException"/> on failure.
/// </summary>
public interface IEventPublisher
{
    /// <exception cref="Exceptions.EventPublishException">Thrown when publishing fails after all retry attempts.</exception>
    public Task PublishSyndicationFeedEventsAsync(string subject,
        IReadOnlyCollection<SyndicationFeedSource> sourceDataItems);

    /// <exception cref="Exceptions.EventPublishException">Thrown when publishing fails after all retry attempts.</exception>
    public Task PublishYouTubeEventsAsync(string subject,
        IReadOnlyCollection<YouTubeSource> youTubeSourceDataItems);

    /// <exception cref="Exceptions.EventPublishException">Thrown when publishing fails after all retry attempts.</exception>
    public Task PublishSpeakingEngagementEventsAsync(string subject,
        IReadOnlyCollection<Engagement> engagements);

    /// <exception cref="Exceptions.EventPublishException">Thrown when publishing fails after all retry attempts.</exception>
    public Task PublishScheduledItemFiredEventsAsync(string subject,
        IReadOnlyCollection<ScheduledItem> scheduledItems);

    /// <exception cref="Exceptions.EventPublishException">Thrown when publishing fails after all retry attempts.</exception>
    public Task PublishRandomPostsEventsAsync(string subject,
        int randomPostId);
}
