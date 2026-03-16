using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class SendTweet(ITwitterManager twitterManager, ILogger<SendTweet> logger)
{

    [Function(ConfigurationFunctionNames.TwitterSendTweet)]
    public async Task Run(
        [QueueTrigger(Queues.TwitterTweetsToSend)] string tweetText)
    {
        try
        {
            var tweetId = await twitterManager.SendTweetAsync(tweetText);
            if (tweetId is null)
            {
                // Log the error
                logger.LogError("Failed to send the tweet: '{TweetText}'. ", tweetText);
            }
            else
            {
                // This is good, just log success
                logger.LogDebug("Posting to Twitter: {TweetText}", tweetText);

                var properties = new Dictionary<string, string>
                {
                    {"message", tweetText},
                    {"id", tweetId}
                };
                logger.LogCustomEvent(Metrics.TwitterPostSent, properties);
            }
        }
        catch (TwitterPostException ex)
        {
            logger.LogError(ex, "Twitter API error sending tweet: '{TweetText}'. Code: {ApiErrorCode}, Message: {ApiErrorMessage}",
                tweetText, ex.ApiErrorCode, ex.ApiErrorMessage);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", tweetText, ex.Message);
            throw;
        }
    }
}