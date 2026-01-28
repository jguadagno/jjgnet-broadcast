// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=facebook_process_scheduled_item_fired

using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Data.Repositories;
using Microsoft.Extensions.Logging;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Extensions.Types;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessScheduledItemFired
{
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly IEngagementManager _engagementManager;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ProcessScheduledItemFired> _logger;
    
    const int MaxFacebookStatusText = 2000;
    
    public ProcessScheduledItemFired(
        SourceDataRepository sourceDataRepository,
        IEngagementManager engagementManager,
        TelemetryClient telemetryClient,
        ILogger<ProcessScheduledItemFired> logger)
    {
        _sourceDataRepository = sourceDataRepository;
        _engagementManager = engagementManager;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_scheduled_item_fired`
    [Function(Constants.ConfigurationFunctionNames.FacebookProcessScheduledItemFired)]
    [QueueOutput(Constants.Queues.FacebookPostStatusToPage)] 
    public async Task<FacebookPostStatus> RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        _logger.LogDebug("Started {FunctionName} at {StartedOn:f}",
            Constants.ConfigurationFunctionNames.FacebookProcessScheduledItemFired, startedOn);
        
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
        
        // Determine what type the post is for
        FacebookPostStatus facebookPostStatus;
        switch (tableEvent.TableName)
        {
            case SourceSystems.SyndicationFeed:
            case SourceSystems.YouTube:
                facebookPostStatus = await GetFacebookPostStatusForSourceData(tableEvent);
                break;
            default:
                facebookPostStatus = await GetFacebookPostStatusForSqlTable(tableEvent);
                break;
        }

        if (facebookPostStatus is null)
        {
            _logger.LogDebug(
                "Could not generate the Facebook post text for {TableName}, {PartitionKey}, {RowKey}",
                tableEvent.TableName, tableEvent.PartitionKey, tableEvent.RowKey);
            return null;
        }
        
        _telemetryClient.TrackEvent(Constants.Metrics.FacebookProcessedScheduledItemFired, new Dictionary<string, string>
        {
            {"tableName", tableEvent.TableName},
            {"partitionKey", tableEvent.PartitionKey},
            {"rowKey", tableEvent.RowKey},
            {"statusText", facebookPostStatus.StatusText}, 
            {"url", facebookPostStatus.LinkUri}
        });
        
        _logger.LogDebug("Generated the Facebook post text for {TableName}, {PartitionKey}, {RowKey}",
            tableEvent.TableName, tableEvent.PartitionKey, tableEvent.RowKey);

        return facebookPostStatus;
    }
    
    private async Task<FacebookPostStatus> GetFacebookPostStatusForSourceData(TableEvent tableEvent)
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
        
        var postTitle = sourceData.Title;
        var hashTagList = HashTagList(sourceData.Tags);
        
        if (statusText.Length + postTitle.Length + 3 + hashTagList.Length >= MaxFacebookStatusText)
        {
            var newLength = MaxFacebookStatusText - statusText.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  $"{statusText} {postTitle} {hashTagList}",
            LinkUri = sourceData.Url                
        };

        _logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
    
    private async Task<FacebookPostStatus> GetFacebookPostStatusForSqlTable(TableEvent tableEvent)
    {
        if (tableEvent is null)
        {
            return null;
        }

        FacebookPostStatus facebookPostStatus = null;
        switch (tableEvent.TableName)
        {
            case SourceSystems.Engagements:
                var engagement = await _engagementManager.GetAsync(tableEvent.PartitionKey.To<int>());
                facebookPostStatus = GetFacebookPostStatusForEngagement(engagement);
                break;
            case SourceSystems.Talks:
                var talk = await _engagementManager.GetTalkAsync(tableEvent.PartitionKey.To<int>());
                facebookPostStatus = GetFacebookPostStatusForTalk(talk);
                break;
                
        }

        return facebookPostStatus;
    }

    private FacebookPostStatus GetFacebookPostStatusForEngagement(Engagement engagement)
    {
        // TODO: Account for custom images for engagement
        // TODO: Account for custom message for engagement
        //  i.e: Join me tomorrow, Join me next week
        // TODO: Maybe handle timezone?
        if (engagement is null)
        {
            return null;
        }
        
        var statusText = $"I'm speaking at {engagement.Name} ({engagement.Url}) starting on {engagement.StartDateTime:f}\n";
        var comments = engagement.Comments;
        statusText += comments;
        
        if (statusText.Length + comments.Length + 1 >= MaxFacebookStatusText)
        {
            var newLength = MaxFacebookStatusText - statusText.Length - comments.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  statusText,
            LinkUri = engagement.Url                
        };

        _logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
    
    private FacebookPostStatus GetFacebookPostStatusForTalk(Talk talk)
    {
        if (talk is null)
        {
            return null;
        }
        // TODO: Account for custom images for talk
        // TODO: Account for custom message for talk
        //  i.e: Join me tomorrow, Join me next week, "Up next in room...", "Join me today..."
        // TODO: Maybe handle timezone?
        
        var statusText = $"Talk: {talk.Name} ({talk.UrlForTalk}) starting on {talk.StartDateTime:f} to {talk.EndDateTime:t}";
        if (talk.TalkLocation is not null)
        {
            statusText += $" in room {talk.TalkLocation}";
        }
        var comments = " Comments: {engagement.Comments}";
        statusText += comments;
        
        if (statusText.Length + comments.Length + 1 >= MaxFacebookStatusText)
        {
            var newLength = MaxFacebookStatusText - statusText.Length - comments.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  statusText,
            LinkUri = talk.UrlForConferenceTalk                
        };

        _logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
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