using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using LinqToTwitter;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class SendTweet(TwitterContext twitterContext, TelemetryClient telemetryClient, ILogger<SendTweet> logger)
{
    private readonly ILogger<SendTweet> _logger = logger;

    [Function("twitter_send_tweet")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.TwitterTweetsToSend)] string tweetText)
    {
        try
        {
            var tweet = await twitterContext.TweetAsync(tweetText);
            if (tweet is null)
            {
                // Log the error
                _logger.LogError("Failed to send the tweet: '{TweetText}'. ", tweetText);
            }
            else
            {
                // This is good, just log success
                telemetryClient.TrackEvent(Constants.Metrics.RandomTweetSent, new Dictionary<string, string>
                {
                    {"message", tweetText} 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", tweetText, ex.Message);
        }
    }
}