using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class SendTweet(ITwitterManager twitterManager, IUserPublisherTwitterSettingsManager twitterSettingsManager, ILogger<SendTweet> logger)
{

    [Function(ConfigurationFunctionNames.TwitterSendTweet)]
    public async Task Run(
        [QueueTrigger(Queues.TwitterTweetsToSend)] SocialMediaPublishRequest request)
    {
        if (string.IsNullOrEmpty(request.OwnerEntraOid))
        {
            logger.LogWarning("Tweet message missing OwnerEntraOid. Skipping");
            return;
        }

        var ownerOid = request.OwnerEntraOid;
        var consumerKey = await twitterSettingsManager.GetConsumerKeyAsync(ownerOid);
        var consumerSecret = await twitterSettingsManager.GetConsumerSecretAsync(ownerOid);
        var accessToken = await twitterSettingsManager.GetAccessTokenAsync(ownerOid);
        var accessTokenSecret = await twitterSettingsManager.GetAccessTokenSecretAsync(ownerOid);

        if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret)
            || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(accessTokenSecret))
        {
            logger.LogWarning("Twitter credentials not found for owner '{OwnerOid}'. Skipping",
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        request.ConsumerKey = consumerKey;
        request.ConsumerSecret = consumerSecret;
        request.AccessToken = accessToken;
        request.AccessTokenSecret = accessTokenSecret;

        try
        {
            if (!string.IsNullOrEmpty(request.ImageUrl))
                logger.LogWarning(
                    "ImageUrl '{ImageUrl}' was present in the tweet message but Twitter media API image upload is not yet implemented. The tweet will be posted without an image attachment",
                    request.ImageUrl);

            var tweetId = await twitterManager.DispatchAsync(request);
            if (tweetId is null)
            {
                logger.LogError("Failed to send the tweet: '{TweetText}'. ", request.Text);
            }
            else
            {
                logger.LogDebug("Posting to Twitter: {TweetText}", request.Text);

                var properties = new Dictionary<string, string>
                {
                    {"message", request.Text},
                    {"id", tweetId}
                };
                logger.LogCustomEvent(Metrics.TwitterPostSent, properties);
            }
        }
        catch (TwitterPostException ex)
        {
            logger.LogError(ex, "Twitter API error sending tweet: '{TweetText}'. Code: {ApiErrorCode}, Message: {ApiErrorMessage}",
                request.Text, ex.ApiErrorCode, ex.ApiErrorMessage);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", request.Text, ex.Message);
            throw;
        }
    }
}
