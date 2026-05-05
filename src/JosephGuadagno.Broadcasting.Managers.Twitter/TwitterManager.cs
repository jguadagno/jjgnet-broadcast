using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Twitter;

public class TwitterManager(TwitterContext twitterContext, ILogger<TwitterManager> logger)
    : ITwitterManager
{
    public Task<string?> PublishAsync(SocialMediaPublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);
        return SendTweetAsync(request.Text);
    }

    public async Task<string?> SendTweetAsync(string tweetText)
    {
        try
        {
            var tweet = await TweetAsync(tweetText);
            if (tweet is null)
            {
                logger.LogError("Failed to send the tweet: '{TweetText}'.", tweetText);
                throw new TwitterPostException($"Failed to send tweet: '{tweetText}'.");
            }

            logger.LogDebug("Tweet sent successfully. Id: '{TweetId}'", tweet.ID);
            return tweet.ID;
        }
        catch (TwitterPostException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", tweetText, ex.Message);
            throw new TwitterPostException($"Failed to send tweet: '{tweetText}'.", ex);
        }
    }

    public Task<string> ComposeMessageAsync(
        ScheduledItem scheduledItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduledItem);
        return Task.FromResult(scheduledItem.Message);
    }

    protected virtual async Task<Tweet?> TweetAsync(string tweetText)
    {
        return await twitterContext.TweetAsync(tweetText);
    }
}