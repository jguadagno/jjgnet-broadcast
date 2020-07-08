using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using LinqToTwitter;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Settings = JosephGuadagno.Broadcasting.Twitter.Models.Settings;

namespace JosephGuadagno.Broadcasting.Twitter.Functions
{
    public static class SendTweet
    {
        [FunctionName("twitter_send_tweet")]
        public static async Task Run([QueueTrigger(Constants.Queues.TwitterTweetsToSend, Connection = Settings.StorageAccount)]string tweetText, ILogger log)
        {
            SingleUserAuthorizer authorizer = new SingleUserAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore
                {
                    ConsumerKey = Environment.GetEnvironmentVariable(Settings.TwitterApiKey),
                    ConsumerSecret = Environment.GetEnvironmentVariable(Settings.TwitterApiSecret),
                    OAuthToken = Environment.GetEnvironmentVariable(Settings.TwitterAccessToken),
                    OAuthTokenSecret =Environment. GetEnvironmentVariable(Settings.TwitterAccessTokenSecret)
                }
            };

            var twitterCtx = new TwitterContext(authorizer);
            await twitterCtx.TweetAsync(tweetText);
        }
    }
}
