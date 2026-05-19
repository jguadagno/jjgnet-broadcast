using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    IEngagementManager engagementManager,
    ISyndicationFeedItemManager syndicationFeedItemManager,
    IYouTubeItemManager youTubeItemManager,
    IMessageTemplateManager messageTemplateManager,
    IPostComposer postComposer,
    ILogger<ProcessScheduledItemFired> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInProcessScheduledItemFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<SocialMediaPublishRequest?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        logger.LogDebug("Started {FunctionName} at {StartedOn:f}",
            ConfigurationFunctionNames.LinkedInProcessScheduledItemFired, startedOn);

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

            var ownerEntraOid = scheduledItem.CreatedByEntraOid ?? string.Empty;
            if (string.IsNullOrEmpty(ownerEntraOid))
            {
                logger.LogWarning("No owner OID on scheduled item {Id} — skipping LinkedIn post",
                    scheduledItem.Id);
                return null;
            }

            var request = await BuildRequestForScheduledItemAsync(scheduledItem, ownerEntraOid);
            if (request is null)
                return null;

            var template = await messageTemplateManager.GetAsync(
                MessageTemplates.Platforms.LinkedIn,
                MessageTemplates.MessageTypes.ScheduledItem,
                ownerEntraOid);
            if (template is null)
                return null;

            var composedText = await postComposer.ComposeAsync(request, template.Template);
            if (string.IsNullOrWhiteSpace(composedText))
            {
                logger.LogWarning("Compose returned empty for scheduled item {Id}", scheduledItem.Id);
                return null;
            }

            var properties = new Dictionary<string, string>
            {
                { "tableName", scheduledItem.ItemTableName },
                { "id", scheduledItem.ItemPrimaryKey.ToString() },
                { "text", composedText },
                { "url", request.LinkUrl ?? "" },
                { "title", request.Title ?? "" }
            };
            logger.LogCustomEvent(Metrics.LinkedInProcessedScheduledItemFired, properties);
            logger.LogDebug("Generated the LinkedIn post text for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);

            request.Text = composedText;
            return request;
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
                ConfigurationFunctionNames.LinkedInProcessScheduledItemFired, endedOn, endedOn - startedOn);
        }
    }

    private async Task<SocialMediaPublishRequest?> BuildRequestForScheduledItemAsync(
        ScheduledItem scheduledItem, string ownerEntraOid)
    {
        switch (scheduledItem.ItemType)
        {
            case ScheduledItemType.SyndicationFeedItems:
                var feedItem = await syndicationFeedItemManager.GetAsync(scheduledItem.ItemPrimaryKey);
                return new SocialMediaPublishRequest
                {
                    Text = "",
                    Title = feedItem.Title,
                    LinkUrl = feedItem.Url,
                    ShortenedUrl = feedItem.ShortenedUrl,
                    Hashtags = feedItem.Tags.Count > 0 ? feedItem.Tags.ToList() : null,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerEntraOid
                };
            case ScheduledItemType.YouTubeItems:
                var ytItem = await youTubeItemManager.GetAsync(scheduledItem.ItemPrimaryKey);
                return new SocialMediaPublishRequest
                {
                    Text = "",
                    Title = ytItem.Title,
                    LinkUrl = ytItem.Url,
                    ShortenedUrl = ytItem.ShortenedUrl,
                    Hashtags = ytItem.Tags.Count > 0 ? ytItem.Tags.ToList() : null,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerEntraOid
                };
            case ScheduledItemType.Engagements:
                var engagement = await engagementManager.GetAsync(scheduledItem.ItemPrimaryKey);
                return new SocialMediaPublishRequest
                {
                    Text = "",
                    Title = engagement.Name,
                    LinkUrl = engagement.Url,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerEntraOid
                };
            case ScheduledItemType.Talks:
                var talk = await engagementManager.GetTalkAsync(scheduledItem.ItemPrimaryKey);
                return new SocialMediaPublishRequest
                {
                    Text = "",
                    Title = talk.Name,
                    LinkUrl = talk.UrlForConferenceTalk,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerEntraOid
                };
            default:
                logger.LogError("The table name '{TableName}' is not supported",
                    LogSanitizer.Sanitize(scheduledItem.ItemTableName));
                return null;
        }
    }
}
