namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ITwitterManager
    : ISocialMediaPublisher
{
    Task<string?> SendTweetAsync(string tweetText);
}
