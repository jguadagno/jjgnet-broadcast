// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=facebook_process_scheduled_item_fired

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Extensions.Types;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessScheduledItemFired
{
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly IEngagementManager _engagementManager;
    private readonly ILogger<ProcessScheduledItemFired> _logger;
    
    const int MaxTweetLength = 240;
    
    public ProcessScheduledItemFired(
        SourceDataRepository sourceDataRepository,
        IEngagementManager engagementManager,
        ILogger<ProcessScheduledItemFired> logger)
    {
        _sourceDataRepository = sourceDataRepository;
        _engagementManager = engagementManager;
        _logger = logger;
    }
    
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_scheduled_item_fired`
    [FunctionName("twitter_process_scheduled_item_fired")]
    public async Task RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        [Queue(Constants.Queues.TwitterTweetsToSend)] ICollector<string> outboundMessages)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.TwitterProcessScheduledItemFired, startedAt);
        
        // Get the Source Data identifier for the event
        if (eventGridEvent.Data is null)
        {
            _logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return;
        }

        var eventGridData = eventGridEvent.Data.ToString();
        if (eventGridData is null)
        {
            _logger.LogError("Failed to retrieve the value of the eventGrid for event '{Id}'", eventGridEvent.Id);
            return;
        }
        
        var tableEvent = JsonSerializer.Deserialize<TableEvent>(eventGridData);
        if (tableEvent == null)
        {
            _logger.LogError("Failed to parse the TableEvent data for event '{Id}'", eventGridEvent.Id);
            return;
        }
        
        // Determine what type the post is for
        string tweetText;
        switch (tableEvent.TableName)
        {
            case SourceSystems.SyndicationFeed:
            case SourceSystems.YouTube:
                tweetText = await GetTweetForSourceData(tableEvent);
                break;
            default:
                tweetText = await GetTweetForSqlTable(tableEvent);
                break;
        }

        if (tweetText is null)
        {
            _logger.LogDebug(
                "Could not generate the tweet for {TableName}, {PartitionKey}, {RowKey}",
                tableEvent.TableName, tableEvent.PartitionKey, tableEvent.RowKey);
            return;
        }
        
        outboundMessages.Add(tweetText);
        _logger.LogDebug("Generated the tweet for {TableName}, {PartitionKey}, {RowKey}",
            tableEvent.TableName, tableEvent.PartitionKey, tableEvent.RowKey);
    }
    
    private async Task<string> GetTweetForSourceData(TableEvent tableEvent)
    {
        if (tableEvent is null)
        {
            return null;
        }

        var statusText = "ICYMI: ";
        var sourceData = await _sourceDataRepository.GetAsync(tableEvent.PartitionKey, tableEvent.RowKey);
        if (sourceData is null)
        {
            _logger.LogWarning("Record for '{PartitionKey}', '{RowKey}' was not found",
                tableEvent.TableName, tableEvent.PartitionKey);
            return null;
        }

        statusText = sourceData.SourceSystem switch
        {
            SourceSystems.SyndicationFeed => "Blog Post: ",
            SourceSystems.YouTube => "Video: ",
            _ => statusText
        };
        
        var url = sourceData.ShortenedUrl ?? sourceData.Url;
        var postTitle = sourceData.Title;
        var hashTagList = HashTagList(sourceData.Tags);
        
        if (statusText.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= MaxTweetLength)
        {
            var newLength = MaxTweetLength - statusText.Length - url.Length - hashTagList.Length - 1;
            postTitle = string.Concat(postTitle.AsSpan(0, newLength - 4), "...");
        }
        
        var tweet = $"{statusText} {postTitle} {url} {hashTagList}";
        
        _logger.LogDebug("Composed tweet '{Tweet}'", tweet);
        return statusText;
    }
    
    private async Task<string> GetTweetForSqlTable(TableEvent tableEvent)
    {
        if (tableEvent is null)
        {
            return null;
        }

        string tweetText = null;
        Engagement engagement;
        switch (tableEvent.TableName)
        {
            case SourceSystems.Engagements:
                engagement = await _engagementManager.GetAsync(tableEvent.PartitionKey.To<int>());
                tweetText = GetTweetForEngagement(engagement);
                break;
            case SourceSystems.Talks:
                var talk = await _engagementManager.GetTalkAsync(tableEvent.PartitionKey.To<int>());
                engagement = await _engagementManager.GetAsync(talk.Id);
                tweetText = GetTweetForTalk(talk, engagement);
                break;
        }

        return tweetText;
    }

    private string GetTweetForEngagement(Engagement engagement)
    {
        // TODO: Account for custom images for engagement
        // TODO: Account for custom message for engagement
        //  i.e: Join me tomorrow, Join me next week
        // TODO: Maybe handle timezone?
        if (engagement is null)
        {
            return null;
        }
        
        var statusText = $"I'm speaking at {engagement.Name} ({engagement.Url}) starting on {engagement.StartDateTime:f}";
        var comments = engagement.Comments;
        statusText += " " + comments;
        
        if (statusText.Length + comments.Length + 1 >= MaxTweetLength)
        {
            var newLength = MaxTweetLength - statusText.Length - comments.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
        
        _logger.LogDebug("Composed tweet '{StatusText}'", statusText);
        return statusText;
    }
    
    private string GetTweetForTalk(Talk talk, Engagement engagement)
    {
        if (talk is null)
        {
            return null;
        }
        // TODO: Account for custom images for talk
        // TODO: Account for custom message for talk
        //  i.e: Join me tomorrow, Join me next week, "Up next in room...", "Join me today..."
        // TODO: Maybe handle timezone?

        var statusText = $"My talk: {talk.Name} ({talk.UrlForTalk})";
        if (engagement is not null)
        {
            statusText += " at " + engagement.Name;
        }
        statusText += $" is starting at {talk.StartDateTime:f}";
        if (talk.TalkLocation is not null)
        {
            statusText += $" in room {talk.TalkLocation}";
        }

        statusText += " Come see it!";
        if (engagement?.Comments is not null)
        {
            statusText += " Comments" + engagement.Comments;
        }
        
        if (statusText.Length + 1 >= MaxTweetLength)
        {
            var newLength = MaxTweetLength - statusText.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        _logger.LogDebug("Composed tweet '{StatusText}'", statusText);
        return statusText;
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