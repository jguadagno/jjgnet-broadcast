using JosephGuadagno.Broadcasting.Domain.Constants;
using LinqToTwitter;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class SendTweet(TwitterContext twitterContext, TelemetryClient telemetryClient, ILogger<SendTweet> logger)
{

    [Function(ConfigurationFunctionNames.TwitterSendTweet)]
    public async Task Run(
        [QueueTrigger(Queues.TwitterTweetsToSend)] string tweetText)
    {
        try
        {
            var tweet = await twitterContext.TweetAsync(tweetText);
            if (tweet is null)
            {
                // Log the error
                logger.LogError("Failed to send the tweet: '{TweetText}'. ", tweetText);
            }
            else
            {
                // This is good, just log success
                logger.LogDebug("Posting to Twitter: {tweetText}", tweetText);
                telemetryClient.TrackEvent(Metrics.TwitterPostSent, new Dictionary<string, string>
                {
                    {"message", tweetText},
                    {"id", tweet.ID}
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", tweetText, ex.Message);
        }
    }
}