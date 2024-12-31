// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=linkedin_process_scheduled_item_fired

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Data.Repositories;
using Microsoft.Extensions.Logging;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using JosephGuadagno.Extensions.Types;
using Microsoft.Azure.Functions.Worker;
using Microsoft.IdentityModel.Abstractions;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessScheduledItemFired
{
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly IEngagementManager _engagementManager;
    private readonly ILinkedInApplicationSettings _linkedInApplicationSettings;
    private readonly ITelemetryClient _telemetryClient;
    private readonly ILogger<ProcessScheduledItemFired> _logger;
    
    const int MaxLinkedInStatusText = 2000;
    
    public ProcessScheduledItemFired(
        SourceDataRepository sourceDataRepository,
        IEngagementManager engagementManager,
        ILinkedInApplicationSettings linkedInApplicationSettings,
        ITelemetryClient telemetryClient,
        ILogger<ProcessScheduledItemFired> logger)
    {
        _sourceDataRepository = sourceDataRepository;
        _engagementManager = engagementManager;
        _linkedInApplicationSettings = linkedInApplicationSettings;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=linkedin_process_scheduled_item_fired`
    [Function("linkedin_process_scheduled_item_fired")]
    [QueueOutput(Constants.Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink> RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        _logger.LogDebug("Started {FunctionName} at {StartedOn:f}",
            Constants.ConfigurationFunctionNames.LinkedInProcessScheduledItemFired, startedOn);
        
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
        
        _logger.LogDebug("Processing the event '{Id}' for '{TableName}', '{PartitionKey}', '{RowKey}'",
            eventGridEvent.Id, tableEvent.TableName, tableEvent.PartitionKey, tableEvent.RowKey);
        
        // Determine what type the post is for
        LinkedInPostLink linkedInPostLink;
        switch (tableEvent.TableName)
        {
            case SourceSystems.SyndicationFeed:
            case SourceSystems.YouTube:
                linkedInPostLink = await GetLinkedInPostLinkForSourceData(tableEvent);
                break;
            default:
                linkedInPostLink = await GetLinkedInPostLinkForSqlTable(tableEvent);
                break;
        }

        if (linkedInPostLink is null)
        {
            _logger.LogError(
                "Could not generate the LinkedIn post link for {TableName}, {PartitionKey}, {RowKey}, linkedInPostLink was null",
                tableEvent.TableName, tableEvent.PartitionKey, tableEvent.RowKey);
            return null;
        }
        
        _telemetryClient.TrackEvent(Constants.Metrics.LinkedInProcessedScheduledItemFired, new Dictionary<string, string>
        {
            {"tableName", tableEvent.TableName},
            {"partitionKey", tableEvent.PartitionKey},
            {"rowKey", tableEvent.RowKey},
            {"text", linkedInPostLink.Text}, 
            {"url", linkedInPostLink.LinkUrl},
            {"title", linkedInPostLink.Title}
        });
        _logger.LogDebug("Generated the LinkedIn post text for {TableName}, {PartitionKey}, {RowKey}",
            tableEvent.TableName, tableEvent.PartitionKey, tableEvent.RowKey);
        return linkedInPostLink;
    }
    
    private async Task<LinkedInPostLink> GetLinkedInPostLinkForSourceData(TableEvent tableEvent)
    {
        if (tableEvent is null)
        {
            _logger.LogError("The table event was null");
            return null;
        }

        var sourceData = await _sourceDataRepository.GetAsync(tableEvent.PartitionKey, tableEvent.RowKey);
        if (sourceData is null)
        {
            _logger.LogWarning("Record for '{PartitionKey}', '{RowKey}' was not found",
                tableEvent.TableName, tableEvent.PartitionKey);
            return null;
        }

        var statusText = sourceData.SourceSystem switch
        {
            SourceSystems.SyndicationFeed => "Blog Post: ",
            SourceSystems.YouTube => "Video: ",
            _ => string.Empty
        };
        
        var url = sourceData.ShortenedUrl ?? sourceData.Url;
        var post = new LinkedInPostLink
        {
            Text = $"{statusText} {sourceData.Title} {HashTagList(sourceData.Tags)}",
            Title = sourceData.Title,
            LinkUrl = url,
            AuthorId = _linkedInApplicationSettings.AuthorId,
            AccessToken = _linkedInApplicationSettings.AccessToken
        };

        _logger.LogDebug("Composed LinkedIn status for '{PartitionKey}', '{RowKey}', '{@Post}'", sourceData.PartitionKey, sourceData.RowKey, post);
        return post;
    }
    
    private async Task<LinkedInPostLink> GetLinkedInPostLinkForSqlTable(TableEvent tableEvent)
    {
        if (tableEvent is null)
        {
            _logger.LogError("The table event was null");
            return null;
        }

        LinkedInPostLink linkedInPostStatusForSqlTable;
        switch (tableEvent.TableName)
        {
            case SourceSystems.Engagements:
                _logger.LogDebug("Getting the engagement for '{PartitionKey}'", tableEvent.PartitionKey);
                var engagement = await _engagementManager.GetAsync(tableEvent.PartitionKey.To<int>());
                if (engagement is null)
                {
                    _logger.LogError("Could not find the engagement for '{PartitionKey}'", tableEvent.PartitionKey);
                    return null;
                }
                linkedInPostStatusForSqlTable = GetLinkedInPostLinkForEngagement(engagement);
                break;
            case SourceSystems.Talks:
                _logger.LogDebug("Getting the talk for '{PartitionKey}'", tableEvent.PartitionKey);
                var talk = await _engagementManager.GetTalkAsync(tableEvent.PartitionKey.To<int>());
                if (talk is null)
                {
                    _logger.LogError("Could not find the talk for '{PartitionKey}'", tableEvent.PartitionKey);
                    return null;
                }
                linkedInPostStatusForSqlTable = GetLinkedInPostLinkForTalk(talk);
                break;
            default:
                _logger.LogError("The table name '{TableName}' is not supported", tableEvent.TableName);
                return null;
        }

        return linkedInPostStatusForSqlTable;
    }

    private LinkedInPostLink GetLinkedInPostLinkForEngagement(Engagement engagement)
    {
        // TODO: Account for custom images for engagement
        // TODO: Account for custom message for engagement
        //  i.e: Join me tomorrow, Join me next week
        // TODO: Maybe handle timezone?
        if (engagement is null)
        {
            _logger.LogError("The engagement was null");
            return null;
        }
        
        var statusText = $"I'm speaking at {engagement.Name} ({engagement.Url}) starting on {engagement.StartDateTime:f}\n";
        var comments = engagement.Comments;
        statusText += comments;
        
        if (statusText.Length >= MaxLinkedInStatusText)
        {
            var newLength = MaxLinkedInStatusText - statusText.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        var post = new LinkedInPostLink
        {
            Text = statusText,
            Title = engagement.Name,
            LinkUrl = engagement.Url,
            AuthorId = _linkedInApplicationSettings.AuthorId,
            AccessToken = _linkedInApplicationSettings.AccessToken
        };

        _logger.LogDebug(
            "Composed LinkedIn status for '{EngagementName}', '{EngagementStartDateTime}', '{@Engagement}', '{@Post}'",
            engagement.Name, engagement.StartDateTime, engagement, post);
        return post;
    }
    
    private LinkedInPostLink GetLinkedInPostLinkForTalk(Talk talk)
    {
        if (talk is null)
        {
            _logger.LogError("The talk was null");
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
        
        if (statusText.Length + comments.Length + 1 >= MaxLinkedInStatusText)
        {
            var newLength = MaxLinkedInStatusText - statusText.Length - comments.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        var post = new LinkedInPostLink
        {
            Text = statusText,
            Title = talk.Name,
            LinkUrl = talk.UrlForTalk,
            AuthorId = _linkedInApplicationSettings.AuthorId,
            AccessToken = _linkedInApplicationSettings.AccessToken
        };

        _logger.LogDebug(
            "Composed LinkedIn status for '{TalkName}', '{TalkStartDateTime}', '{@Talk}', '{@Post}'",
            talk.Name, talk.StartDateTime, talk, post);
        return post;
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