using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ITwitterManager
    : ISocialMediaPublisher
{
    Task<string?> SendTweetAsync(string tweetText);

    Task<string> ComposeMessageAsync(ScheduledItem scheduledItem, CancellationToken cancellationToken = default);
}
