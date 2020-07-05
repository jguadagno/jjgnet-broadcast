using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.ServiceModel.Syndication;
using System.Xml;
using JosephGuadagno.Utilities.Web.Shortener.Models;

namespace JosephGuadagno.Broadcasting.Twitter
{
    public static class ScheduleNewBlogPost
    {
        [FunctionName("tweet_new_blog_post")]
        public static async Task RunAsync(
            [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, 
            [Queue(Constants.Queues.TwitterTweetsToSend, Connection = Constants.Settings.StorageAccount)] ICollector<string> outboundMessages,
            ILogger log)
        {
            var startedAt = DateTime.Now;
            
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");

            var configurationHelper = new ConfigurationHelper(
                Environment.GetEnvironmentVariable(Constants.Settings.StorageAccount), Constants.Tables.Configuration);
            var configuration = await configurationHelper.GetConfigurationAsync() ?? new Configuration {LastCheckedFeed = DateTime.Now}; ;
            
            var url = Environment.GetEnvironmentVariable(Constants.Settings.FeedUrl);
            log.LogInformation($"Checking '{url}' for posts since '{configuration.LastCheckedFeed}'");
            var newItems = GetPostsSince(url, configuration.LastCheckedFeed);

            if (newItems == null || newItems.Count == 0)
            {
                configuration.LastCheckedFeed = startedAt;
                await configurationHelper.SaveConfigurationAsync(configuration);
                log.LogDebug($"No new post found at '{url}'.");
                return;
            }

            log.LogDebug($"Found {newItems.Count} new post(s).");
            foreach (var item in newItems)
            {
                var tweet = await SendTweet(item);
                if (!string.IsNullOrEmpty(tweet))
                {
                    outboundMessages.Add(tweet);
                }
            }

            configuration.LastCheckedFeed = startedAt;
            await configurationHelper.SaveConfigurationAsync(configuration);
            log.LogDebug("Done.");
        }
        
        private static List<SyndicationItem> GetPostsSince(string url, DateTime lastChecked)
        {
            var items = new List<SyndicationItem>();
            if (string.IsNullOrEmpty(url))
            {
                return items;
            }

            using var reader = XmlReader.Create(url);
            var feed = SyndicationFeed.Load(reader);

            items = feed.Items.Where(i => i.PublishDate >= lastChecked).ToList();
            return items;
        }

        private static async Task<string> SendTweet(SyndicationItem item)
        {
            if (item == null)
            {
                return null;
            }
            
            // Build Tweet
            var tweetStart = "New Blog Post: ";
            var url = await GetShortenedUrlAsync(item.Id);
            string postTitle = item.Title.Text;

            if (tweetStart.Length + url.Length + postTitle.Length + 3 >= 240)
            {
                var newLength = 240 - tweetStart.Length - url.Length - 1;
                postTitle = postTitle.Substring(0, newLength - 4) + "...";
            }
            
            var tweet = $"{tweetStart} {postTitle} {url}";

            return tweet;
        }

        private static async Task<string> GetShortenedUrlAsync(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl))
            {
                return null;
            }

            var bitly = new JosephGuadagno.Utilities.Web.Shortener.Bitly(new HttpClient(),
                new BitlyConfiguration
                {
                    ApiRootUri = Environment.GetEnvironmentVariable(Constants.Settings.BitlyAPIRootUri),
                    Token = Environment.GetEnvironmentVariable(Constants.Settings.BitlyToken)
                });

            var result = await bitly.Shorten(originalUrl, "jjg.me");
            return result == null ? originalUrl : result.Link;
        }
    }
}