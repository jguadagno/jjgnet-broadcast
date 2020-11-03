using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter
{
    public class ProcessNewSourceData
    {
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly ILogger<ProcessNewSourceData> _logger;

        public ProcessNewSourceData(SourceDataRepository sourceDataRepository, ILogger<ProcessNewSourceData> logger)
        {
            _sourceDataRepository = sourceDataRepository;
            _logger = logger;
        }
        
        // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
        // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
        // When debugging locally start ngrok
        // Create a new EventGrid endpoint in Azure similar to
        // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_new_source_data`
        [FunctionName("twitter_process_new_source_data")]
        public async Task RunAsync(
            [EventGridTrigger()] EventGridEvent eventGridEvent,
            [Queue(Constants.Queues.TwitterTweetsToSend)] ICollector<string> outboundMessages)
        {
            // Get the Source Data identifier for the event
            var tableEvent = JsonSerializer.Deserialize<TableEvent>(eventGridEvent.Data.ToString());
            if (tableEvent == null)
            {
                _logger.LogError($"Failed to parse the TableEvent data for event '{eventGridEvent.Id}'");
                return;
            }

            // Create the scheduled tweets for it
            _logger.LogDebug($"Looking for source with fields '{tableEvent.PartitionKey}' and '{tableEvent.RowKey}'");
            var sourceData = await _sourceDataRepository.GetAsync(tableEvent.PartitionKey, tableEvent.RowKey);

            if (sourceData == null)
            {
                _logger.LogWarning($"Record for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}' was NOT found");
                return;
            }
            
            _logger.LogDebug($"Composing tweet for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}'.");
            
            var tweet = ComposeTweet(sourceData);
            if (!string.IsNullOrEmpty(tweet))
            {
                outboundMessages.Add(tweet);
            }
            
            // Done
            _logger.LogDebug($"Done composing tweet for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}'.");
        }
        
        private string ComposeTweet(SourceData item)
        {
            if (item == null)
            {
                return null;
            }

            const int maxTweetLenght = 240;
            
            // Build Tweet
            var tweetStart = "";
            switch (item.SourceSystem)
            {
                case nameof(SourceSystems.SyndicationFeed):
                    tweetStart = "New Blog Post: ";
                    break;
                case nameof(SourceSystems.YouTube):
                    tweetStart = "New Video: ";
                    break;
            }
            
            var url = item.ShortenedUrl ?? item.Url;
            var postTitle = item.Title;
        
            if (tweetStart.Length + url.Length + postTitle.Length + 3 >= maxTweetLenght)
            {
                var newLength = maxTweetLenght - tweetStart.Length - url.Length - 1;
                postTitle = postTitle.Substring(0, newLength - 4) + "...";
            }
            
            var tweet = $"{tweetStart} {postTitle} {url}";
            _logger.LogDebug($"Composed tweet '{tweet}'", tweet);
            
            return tweet;
        }
    }
}