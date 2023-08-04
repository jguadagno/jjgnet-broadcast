using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using LinqToTwitter;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class SendTweet
{
    private readonly TwitterContext _twitterContext;
        
    public SendTweet(TwitterContext twitterContext)
    {
        _twitterContext = twitterContext;
    }
        
    [FunctionName("twitter_send_tweet")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.TwitterTweetsToSend)]
        string tweetText,
        ILogger log)
    {
        try
        {
            var tweet = await _twitterContext.TweetAsync(tweetText);
            if (tweet is null)
            {
                // Log the error
                log.LogError($"Failed to send the tweet: '{tweetText}'. ", tweetText);
            }
            else
            {
                // This is good, just log success
            }
        }
        catch (Exception ex)
        {
            log.LogError($"Failed to send the tweet: '{tweetText}'. Exception: '{ex.Message}'", ex, tweetText);
        }
    }
}