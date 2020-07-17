using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using LinqToTwitter;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter
{
    public class SendTweet
    {
        private readonly ISettings _settings;
        private readonly TwitterContext _twitterContext;
        
        public SendTweet(ISettings settings, TwitterContext twitterContext)
        {
            _settings = settings;
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
}
