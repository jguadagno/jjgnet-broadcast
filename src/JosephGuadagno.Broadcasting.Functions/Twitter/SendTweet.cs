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
        await _twitterContext.TweetAsync(tweetText);
    }
}