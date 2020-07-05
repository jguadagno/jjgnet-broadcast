using System;
using System.Threading.Tasks;
using LinqToTwitter;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Twitter
{
    public static class SendTweet
    {
        [FunctionName("twitter_send_tweet")]
        public static async Task Run([QueueTrigger(Constants.Queues.TwitterTweetsToSend, Connection = "AzureWebJobsStorage")]string tweetText, ILogger log)
        {
            SingleUserAuthorizer authorizer = new SingleUserAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore
                {
                    ConsumerKey = Environment.GetEnvironmentVariable(Constants.Settings.TwitterApiKey),
                    ConsumerSecret = Environment.GetEnvironmentVariable(Constants.Settings.TwitterApiSecret),
                    OAuthToken = Environment.GetEnvironmentVariable(Constants.Settings.TwitterAccessToken),
                    OAuthTokenSecret =Environment. GetEnvironmentVariable(Constants.Settings.TwitterAccessTokenSecret)
                }
            };

            var twitterCtx = new TwitterContext(authorizer);
            await twitterCtx.TweetAsync(tweetText);
        }
    }
}
