using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEventPublisher
{
    public Task<bool> PublishEventsAsync(string topicUrl, string topicKey, string subject,
        IReadOnlyCollection<SourceData> sourceDataItems);

    public Task<bool> PublishEventsAsync(string topicUrl, string topicKey, string subject,
        IReadOnlyCollection<ScheduledItem> scheduledItems );

    public Task<bool> PublishEventsAsync(string topicUrl, string topicKey, string subject,
        string randomPostId);
}