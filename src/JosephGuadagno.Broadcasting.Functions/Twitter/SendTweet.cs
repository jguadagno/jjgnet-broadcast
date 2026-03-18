using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class SendTweet(ITwitterManager twitterManager, ILogger<SendTweet> logger)
{

    [Function(ConfigurationFunctionNames.TwitterSendTweet)]
    public async Task Run(
        [QueueTrigger(Queues.TwitterTweetsToSend)] TwitterTweetMessage tweetMessage)
    {
        try
        {
            if (!string.IsNullOrEmpty(tweetMessage.ImageUrl))
                logger.LogWarning(
                    "ImageUrl '{ImageUrl}' was present in the tweet message but Twitter media API image upload is not yet implemented. The tweet will be posted without an image attachment.",
                    tweetMessage.ImageUrl);

            var tweetId = await twitterManager.SendTweetAsync(tweetMessage.Text);
            if (tweetId is null)
            {
                // Log the error
                logger.LogError("Failed to send the tweet: '{TweetText}'. ", tweetMessage.Text);
            }
            else
            {
                // This is good, just log success
                logger.LogDebug("Posting to Twitter: {TweetText}", tweetMessage.Text);

                var properties = new Dictionary<string, string>
                {
                    {"message", tweetMessage.Text},
                    {"id", tweetId}
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