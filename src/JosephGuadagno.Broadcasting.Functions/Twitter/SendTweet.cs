using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class SendTweet(IUserPublisherSettingManager userPublisherSettingManager, ILogger<SendTweet> logger)
{

    [Function(ConfigurationFunctionNames.TwitterSendTweet)]
    public async Task Run(
        [QueueTrigger(Queues.TwitterTweetsToSend)] TwitterTweetMessage tweetMessage)
    {
        if (string.IsNullOrEmpty(tweetMessage.CreatedByEntraOid))
        {
            logger.LogWarning("Tweet message missing CreatedByEntraOid. Skipping.");
            return;
        }

        var credentials = await userPublisherSettingManager.GetCredentialsAsync(
            tweetMessage.CreatedByEntraOid,
            SocialMediaPlatformIds.Twitter);

        if (!credentials.ContainsKey("ConsumerKey") || !credentials.ContainsKey("ConsumerSecret")
            || !credentials.ContainsKey("OAuthToken") || !credentials.ContainsKey("OAuthTokenSecret"))
        {
            logger.LogWarning("Twitter credentials not found for owner '{OwnerOid}'. Skipping.",
                LogSanitizer.Sanitize(tweetMessage.CreatedByEntraOid));
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(tweetMessage.ImageUrl))
                logger.LogWarning(
                    "ImageUrl '{ImageUrl}' was present in the tweet message but Twitter media API image upload is not yet implemented. The tweet will be posted without an image attachment.",
                    tweetMessage.ImageUrl);

            var credentialStore = new InMemoryCredentialStore
            {
                ConsumerKey = credentials.GetValueOrDefault("ConsumerKey"),
                ConsumerSecret = credentials.GetValueOrDefault("ConsumerSecret"),
                OAuthToken = credentials.GetValueOrDefault("OAuthToken"),
                OAuthTokenSecret = credentials.GetValueOrDefault("OAuthTokenSecret"),
            };
            var authorizer = new SingleUserAuthorizer { CredentialStore = credentialStore };
            var twitterContext = new TwitterContext(authorizer);

            var tweet = await twitterContext.TweetAsync(tweetMessage.Text);
            if (tweet is null)
            {
                logger.LogError("Failed to send the tweet: '{TweetText}'. ", tweetMessage.Text);
            }
            else
            {
                logger.LogDebug("Posting to Twitter: {TweetText}", tweetMessage.Text);

                var properties = new Dictionary<string, string>
                {
                    {"message", tweetMessage.Text},
                    {"id", tweet.ID ?? string.Empty}
                };
                logger.LogCustomEvent(Metrics.TwitterPostSent, properties);
            }
        }
        catch (TwitterPostException ex)
        {
            logger.LogError(ex, "Twitter API error sending tweet: '{TweetText}'. Code: {ApiErrorCode}, Message: {ApiErrorMessage}",
                tweetMessage.Text, ex.ApiErrorCode, ex.ApiErrorMessage);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", tweetMessage.Text, ex.Message);
            throw;
        }
    }
}
