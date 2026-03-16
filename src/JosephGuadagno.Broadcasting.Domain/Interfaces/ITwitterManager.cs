namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ITwitterManager
{
    Task<string?> SendTweetAsync(string tweetText);
}
