using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Twitter;

public class TwitterManager(ILogger<TwitterManager> logger) : ITwitterManager
{
	public async Task<string?> DispatchAsync(SocialMediaPublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);

        try
        {
            var tweet = await TweetAsync(
                request.ConsumerKey ?? string.Empty,
                request.ConsumerSecret ?? string.Empty,
                request.AccessToken ?? string.Empty,
                request.AccessTokenSecret ?? string.Empty,
                request.Text);

            if (tweet is null)
            {
                logger.LogError("Failed to send the tweet: '{TweetText}'", request.Text);
                throw new TwitterPostException($"Failed to send tweet: '{request.Text}'.");
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
            logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", request.Text, ex.Message);
            throw new TwitterPostException($"Failed to send tweet: '{request.Text}'.", ex);
        }
    }

    protected virtual async Task<Tweet?> TweetAsync(
        string consumerKey, string consumerSecret,
        string accessToken, string accessTokenSecret,
        string tweetText)
    {
        var credentialStore = new InMemoryCredentialStore
        {
            ConsumerKey = consumerKey,
            ConsumerSecret = consumerSecret,
            OAuthToken = accessToken,
            OAuthTokenSecret = accessTokenSecret,
        };
        var authorizer = new SingleUserAuthorizer { CredentialStore = credentialStore };
        var context = new TwitterContext(authorizer);
        return await context.TweetAsync(tweetText);
    }
}