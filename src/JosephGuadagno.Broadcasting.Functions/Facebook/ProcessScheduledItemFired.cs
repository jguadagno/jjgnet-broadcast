using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    IEngagementManager engagementManager,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IYouTubeSourceManager youTubeSourceManager,
    IFacebookManager facebookManager,
    ILogger<ProcessScheduledItemFired> logger)
{
    [Function(ConfigurationFunctionNames.FacebookProcessScheduledItemFired)]
    [QueueOutput(Queues.FacebookPostStatusToPage)]
    public async Task<FacebookPostStatus?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        logger.LogDebug("Started {FunctionName} at {StartedOn:f}",
            ConfigurationFunctionNames.FacebookProcessScheduledItemFired, startedOn);

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

            FacebookPostStatus facebookPostStatus;

            switch (scheduledItem.ItemType)
            {
                case ScheduledItemType.Engagements:
                    facebookPostStatus = await GetFacebookPostStatusForEngagement(scheduledItem.ItemPrimaryKey);
                    break;
                case ScheduledItemType.Talks:
                    facebookPostStatus = await GetFacebookPostStatusForTalk(scheduledItem.ItemPrimaryKey);
                    break;
                case ScheduledItemType.SyndicationFeedSources:
                    facebookPostStatus = await GetFacebookPostStatusForSyndicationSource(scheduledItem.ItemPrimaryKey);
                    break;
                case ScheduledItemType.YouTubeSources:
                    facebookPostStatus = await GetFacebookPostStatusForYouTubeSource(scheduledItem.ItemPrimaryKey);
                    break;
                default:
                    logger.LogError("The table name '{TableName}' is not supported", scheduledItem.ItemTableName);
                    return null;
            }

            facebookPostStatus.StatusText = await facebookManager.ComposeMessageAsync(scheduledItem);
            facebookPostStatus.ImageUrl = scheduledItem.ImageUrl;

            var properties = new Dictionary<string, string>
            {
                { "tableName", scheduledItem.ItemTableName },
                { "id", scheduledItem.ItemPrimaryKey.ToString() },
                { "text", facebookPostStatus.StatusText },
                { "url", facebookPostStatus.LinkUri }
            };
            logger.LogCustomEvent(Metrics.FacebookProcessedScheduledItemFired, properties);
            logger.LogDebug("Generated the Facebook status for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);
            return facebookPostStatus;
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
                ConfigurationFunctionNames.FacebookProcessScheduledItemFired, endedOn, endedOn - startedOn);
        }
    }

    private async Task<FacebookPostStatus> GetFacebookPostStatusForSyndicationSource(int primaryKey)
    {
        var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(primaryKey);
        return new FacebookPostStatus { StatusText = string.Empty, LinkUri = syndicationFeedSource.Url };
    }

    private async Task<FacebookPostStatus> GetFacebookPostStatusForYouTubeSource(int primaryKey)
    {
        var youTubeSource = await youTubeSourceManager.GetAsync(primaryKey);
        return new FacebookPostStatus { StatusText = string.Empty, LinkUri = youTubeSource.Url };
    }

    private async Task<FacebookPostStatus> GetFacebookPostStatusForEngagement(int primaryKey)
    {
        var engagement = await engagementManager.GetAsync(primaryKey);
        return new FacebookPostStatus { StatusText = string.Empty, LinkUri = engagement.Url };
    }

    private async Task<FacebookPostStatus> GetFacebookPostStatusForTalk(int primaryKey)
    {
        var talk = await engagementManager.GetTalkAsync(primaryKey);
        return new FacebookPostStatus { StatusText = string.Empty, LinkUri = talk.UrlForConferenceTalk };
    }
}
