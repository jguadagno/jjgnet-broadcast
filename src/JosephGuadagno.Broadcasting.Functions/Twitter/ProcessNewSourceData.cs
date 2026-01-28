using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessNewSourceData
{
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ProcessNewSourceData> _logger;

    public ProcessNewSourceData(SourceDataRepository sourceDataRepository, TelemetryClient telemetryClient, ILogger<ProcessNewSourceData> logger)
    {
        _sourceDataRepository = sourceDataRepository;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
        
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_new_source_data`
    [Function(Constants.ConfigurationFunctionNames.TwitterProcessNewSourceData)]
    [QueueOutput(Constants.Queues.TwitterTweetsToSend)] 
    public async Task<string> RunAsync(
        [EventGridTrigger()] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.TwitterProcessNewSourceData, startedAt);
        
        // Get the Source Data identifier for the event
        if (eventGridEvent.Data is null)
        {
            _logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        var eventGridData = eventGridEvent.Data.ToString();

        var tableEvent = JsonSerializer.Deserialize<TableEvent>(eventGridData);
        if (tableEvent == null)
        {
            _logger.LogError("Failed to parse the TableEvent data for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        // Create the scheduled tweets for it
        _logger.LogDebug("Looking for source with fields '{PartitionKey}' and '{RowKey}'",
            tableEvent.PartitionKey, tableEvent.RowKey);
        var sourceData = await _sourceDataRepository.GetAsync(tableEvent.PartitionKey, tableEvent.RowKey);

        if (sourceData == null)
        {
            _logger.LogWarning("Record for '{PartitionKey}', '{RowKey}' was NOT found",
                tableEvent.PartitionKey, tableEvent.RowKey);
            return null;
        }

        _logger.LogDebug("Composing tweet for '{PartitionKey}', '{RowKey}'",
            tableEvent.PartitionKey, tableEvent.RowKey);
            
        var tweet = ComposeTweet(sourceData);
           
        // Done
        _telemetryClient.TrackEvent(Constants.Metrics.TwitterProcessedNewSourceData, new Dictionary<string, string>
        {
            { "post", tweet },
            { "title", sourceData.Title },
            { "url", sourceData.Url },
            { "sourceSystem", sourceData.SourceSystem },
            { "partitionKey", sourceData.PartitionKey },
            { "rowKey", sourceData.RowKey },
        });
        
        _logger.LogDebug("Done composing tweet for '{PartitionKey}', '{RowKey}'",
            tableEvent.PartitionKey, tableEvent.RowKey);
        return tweet;
    }
        
    private string ComposeTweet(SourceData item)
    {
        if (item == null)
        {
            return null;
        }

        const int maxTweetLength = 240;
            
        // Build Tweet
        var tweetStart = "";
        switch (item.SourceSystem)
        {
            case nameof(SourceSystems.SyndicationFeed):
                tweetStart = item.UpdatedOnDate > item.PublicationDate ? "Updated Blog Post: " : "New Blog Post: ";
                break;
            case nameof(SourceSystems.YouTube):
                tweetStart = item.UpdatedOnDate > item.PublicationDate ? "Updated Video: " : "New Video: ";
                break;
        }
            
        var url = item.ShortenedUrl ?? item.Url;
        var postTitle = item.Title;
        var hashTagList = HashTagList(item.Tags);
        
        if (tweetStart.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= maxTweetLength)
        {
            var newLength = maxTweetLength - tweetStart.Length - url.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }
            
        var tweet = $"{tweetStart} {postTitle} {url} {hashTagList}";
        _logger.LogDebug("Composed tweet '{Tweet}'", tweet);
            
        return tweet;
    }
        
    private string HashTagList(string tags)
    {
        if (string.IsNullOrEmpty(tags))
        {
            return "#dotnet #csharp #dotnetcore";
        }

        var tagList = tags.Split(',');
        var hashTagCategories = tagList.Where(tag => !tag.Contains("Article"));

        return hashTagCategories.Aggregate("",
            (current, tag) => current + $" #{tag.Replace(" ", "").Replace(".", "")}");
    }
}