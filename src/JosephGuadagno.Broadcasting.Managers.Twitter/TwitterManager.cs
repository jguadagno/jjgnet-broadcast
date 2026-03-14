using JosephGuadagno.Broadcasting.Domain.Interfaces;
using LinqToTwitter;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Twitter;

public class TwitterManager(TwitterContext twitterContext, ILogger<TwitterManager> logger)
    : ITwitterManager
{
    public async Task<string?> SendTweetAsync(string tweetText)
    {
        try
        {
            var tweet = await TweetAsync(tweetText);
            if (tweet is null)
            {
                logger.LogError("Failed to send the tweet: '{TweetText}'.", tweetText);
                return null;
            }

            logger.LogDebug("Tweet sent successfully. Id: '{TweetId}'", tweet.ID);
            return tweet.ID;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", tweetText, ex.Message);
            return null;
        }
    }

    protected virtual async Task<Tweet?> TweetAsync(string tweetText)
    {
        return await twitterContext.TweetAsync(tweetText);
    }
}
