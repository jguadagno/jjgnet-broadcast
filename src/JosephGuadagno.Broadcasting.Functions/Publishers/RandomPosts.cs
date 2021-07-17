using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers
{
    public class RandomPosts
    {
        private readonly ISyndicationFeedReader _syndicationFeedReader;
        private readonly ILogger<RandomPosts> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly IRandomPostSettings _randomPostSettings;

        public RandomPosts(ISyndicationFeedReader syndicationFeedReader,
            IRandomPostSettings randomPostSettings,
            ILogger<RandomPosts> logger,
            TelemetryClient telemetryClient)
        {
            _syndicationFeedReader = syndicationFeedReader;
            _randomPostSettings = randomPostSettings;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }
        
        [FunctionName("publishers_random_posts")]
        public async Task RunAsync(
            [TimerTrigger("0 0 9,16 * * *")] TimerInfo myTimer,
            [Queue(Constants.Queues.TwitterTweetsToSend)] ICollector<string> outboundMessages)
        {
            // 0 */2 * * * *
            // 0 0 9,16 * * *
            var startedAt = DateTime.UtcNow;
            _logger.LogDebug(
                $"{Constants.ConfigurationFunctionNames.PublishersRandomPosts} Publisher started at: {{startedAt}}",
                Constants.ConfigurationFunctionNames.PublishersRandomPosts, startedAt);

            // Get the feed items
            // Check for the from date
            var cutoffDate = DateTime.MinValue;
            if (_randomPostSettings.CutoffDate != DateTime.MinValue)
            {
                cutoffDate = _randomPostSettings.CutoffDate;
            }

            _logger.LogInformation($"Getting all items from feed from '{cutoffDate}'", cutoffDate);
            var feedItems = _syndicationFeedReader.GetSyndicationItems(cutoffDate, _randomPostSettings.ExcludedCategories);

            // If there is nothing new, save the last checked value and exit
            if (feedItems == null || feedItems.Count == 0)
            {
                _logger.LogInformation("No posts found in the Json Feed");
                return;
            }
            
            // Pick a Random one
            var randomPost = feedItems
                .OrderBy(p => Guid.NewGuid())
                .FirstOrDefault();

            if (randomPost == null)
            {
                Console.WriteLine("Could not get a post. Exiting");
                return;
            }

            // Build the tweet
            var hashtags = HashTagList(randomPost.Categories);
            var status =
                $"ICYMI: ({randomPost.PublishDate.Date.ToShortDateString()}): \"{randomPost.Title.Text}.\" RTs and feedback are always appreciated! {randomPost.Links[0].Uri} {hashtags}";
            
            // Post the message to the Queue
            outboundMessages.Add(status);
            
            // Return
            var doneMessage = $"Picked a random post '{randomPost.Title}'";
            _telemetryClient.TrackEvent(Constants.Metrics.RandomTweetSent, new Dictionary<string, string>
            {
                {"title", randomPost.Title.Text },
                { "tweet", status}
            });
            _logger.LogDebug(doneMessage);
        }
        
        private static string HashTagList(Collection<SyndicationCategory> categories)
        {
            if (categories is null || categories.Count == 0)
            {
                return "#dotnet #csharp #dotnetcore";
            }

            var hashTagCategories = categories.Where(c => !c.Name.Contains("Articles"));

            return hashTagCategories.Aggregate("",
                (current, category) => current + $" #{category.Name.Replace(" ", "").Replace(".", "")}");
        }
    }
}