using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEventPublisher
{
    public Task<bool> PublishSyndicationFeedEventsAsync(string subject,
        IReadOnlyCollection<SyndicationFeedSource> sourceDataItems);

    public Task<bool> PublishYouTubeEventsAsync(string subject,
        IReadOnlyCollection<YouTubeSource> youTubeSourceDataItems);

    public Task<bool> PublishScheduledItemFiredEventsAsync(string subject,
        IReadOnlyCollection<ScheduledItem> scheduledItems );

    public Task<bool> PublishRandomPostsEventsAsync(string subject,
        int randomPostId);
}