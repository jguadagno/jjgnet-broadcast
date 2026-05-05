using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    IEngagementManager engagementManager,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IYouTubeSourceManager youTubeSourceManager,
    IBlueskyManager blueskyManager,
    ILogger<ProcessScheduledItemFired> logger)
{
    const int MaxPostLength = 300;

    [Function(ConfigurationFunctionNames.BlueskyProcessScheduledItemFired)]
    [QueueOutput(Queues.BlueskyPostToSend)]
    public async Task<BlueskyPostMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        logger.LogDebug("{FunctionName} started at: {StartedOn:f}",
            ConfigurationFunctionNames.BlueskyProcessScheduledItemFired, startedOn);

        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }
        try
        {
            var eventGridData = eventGridEvent.Data.ToString();
            var scheduledItemFiredEvent = JsonSerializer.Deserialize<ScheduledItemFiredEvent>(eventGridData);
            if (scheduledItemFiredEvent is null)
            {
                logger.LogError("Failed to parse the TableEvent data for event '{Id}'", eventGridEvent.Id);
                return null;
            }
            var scheduledItem = await scheduledItemManager.GetAsync(scheduledItemFiredEvent.Id);

            logger.LogDebug("Processing the event '{Id}' for '{TableName}', '{PartitionKey}'",
                eventGridEvent.Id, scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);

            var blueskyPostText = await blueskyManager.ComposeMessageAsync(scheduledItem);

            if (string.IsNullOrWhiteSpace(blueskyPostText))
            {
                logger.LogWarning("No Bluesky post text for scheduled item {Id}", scheduledItem.Id);
                return null;
            }

            var sourceUrl = await GetSourceUrlAsync(scheduledItem);

            var properties = new Dictionary<string, string>
            {
                { "tableName", scheduledItem.ItemTableName },
                { "id", scheduledItem.ItemPrimaryKey.ToString() },
                { "text", blueskyPostText }
            };
            logger.LogCustomEvent(Metrics.BlueskyProcessedScheduledItemFired, properties);
            logger.LogDebug("Generated the BlueSky post text for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);

            return new BlueskyPostMessage
            {
                Text = blueskyPostText.Length > MaxPostLength
                    ? blueskyPostText[..MaxPostLength]
                    : blueskyPostText,
                Url = sourceUrl,
                ImageUrl = scheduledItem.ImageUrl
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new scheduled item. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
        finally
        {
            var endedOn = DateTimeOffset.Now;
            logger.LogDebug("Ended {FunctionName} at {EndedOn:f} with duration {Duration:c}",
                ConfigurationFunctionNames.BlueskyProcessScheduledItemFired, endedOn, endedOn - startedOn);
        }
    }

    private async Task<string?> GetSourceUrlAsync(ScheduledItem scheduledItem)
    {
        return scheduledItem.ItemType switch
        {
            ScheduledItemType.SyndicationFeedSources =>
                (await syndicationFeedSourceManager.GetAsync(scheduledItem.ItemPrimaryKey)).Url,
            ScheduledItemType.YouTubeSources =>
                (await youTubeSourceManager.GetAsync(scheduledItem.ItemPrimaryKey)).Url,
            ScheduledItemType.Engagements =>
                (await engagementManager.GetAsync(scheduledItem.ItemPrimaryKey)).Url,
            ScheduledItemType.Talks =>
                (await engagementManager.GetTalkAsync(scheduledItem.ItemPrimaryKey)).UrlForConferenceTalk,
            _ => null
        };
    }
}