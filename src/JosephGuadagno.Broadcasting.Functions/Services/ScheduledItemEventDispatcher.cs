using System.Text.Json;
using Azure.Storage.Queues;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Services;

public class ScheduledItemEventDispatcher(
    IUserEventDispatcherMappingDataStore userEventDispatcherMappingDataStore,
    IEngagementManager engagementManager,
    ISyndicationFeedItemManager syndicationFeedItemManager,
    IYouTubeItemManager youTubeItemManager,
    IMessageTemplateManager messageTemplateManager,
    IPostComposer postComposer,
    QueueServiceClient queueServiceClient,
    ILogger<ScheduledItemEventDispatcher> logger) : IScheduledItemEventDispatcher
{
    private static readonly Dictionary<int, string> PlatformQueues = new()
    {
        { SocialMediaPlatformIds.Twitter,  Queues.TwitterTweetsToSend },
        { SocialMediaPlatformIds.Bluesky,  Queues.BlueskyPostToSend },
        { SocialMediaPlatformIds.LinkedIn, Queues.LinkedInPostLink },
        { SocialMediaPlatformIds.Facebook, Queues.FacebookPostStatusToPage },
    };

    public async Task DispatchAsync(ScheduledItem scheduledItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduledItem);

        if (string.IsNullOrWhiteSpace(scheduledItem.CreatedByEntraOid))
        {
            logger.LogWarning("No owner OID on scheduled item {ScheduledItemId} — skipping queue dispatch",
                scheduledItem.Id);
            return;
        }

        var ownerOid = scheduledItem.CreatedByEntraOid;
        var mappings = await userEventDispatcherMappingDataStore.GetByUserAndEventTypeAsync(
            ownerOid, MessageTemplates.MessageTypes.ScheduledItem, cancellationToken);

        if (mappings.Count == 0)
        {
            logger.LogDebug("No active dispatcher mappings for owner '{OwnerOid}' / {EventType}",
                LogSanitizer.Sanitize(ownerOid), MessageTemplates.MessageTypes.ScheduledItem);
            return;
        }

        var request = await BuildRequestForScheduledItemAsync(scheduledItem, ownerOid, cancellationToken);
        if (request is null)
        {
            return;
        }

        foreach (var mapping in mappings)
        {
            await PublishToQueueAsync(request, mapping.SocialMediaPlatformId, scheduledItem.Id.ToString(), ownerOid, cancellationToken);
        }
    }

    private async Task<SocialMediaPublishRequest?> BuildRequestForScheduledItemAsync(
        ScheduledItem scheduledItem,
        string ownerOid,
        CancellationToken cancellationToken)
    {
        switch (scheduledItem.ItemType)
        {
            case ScheduledItemType.SyndicationFeedItems:
                var feedItem = await syndicationFeedItemManager.GetAsync(scheduledItem.ItemPrimaryKey, cancellationToken);
                if (feedItem is null)
                {
                    logger.LogWarning("Syndication feed item {ItemId} not found for scheduled item {ScheduledItemId}",
                        scheduledItem.ItemPrimaryKey, scheduledItem.Id);
                    return null;
                }
                return new SocialMediaPublishRequest
                {
                    Text = string.Empty,
                    Title = feedItem.Title,
                    LinkUrl = feedItem.Url,
                    ShortenedUrl = feedItem.ShortenedUrl,
                    Hashtags = feedItem.Tags.Count > 0 ? feedItem.Tags.ToList() : null,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerOid,
                };

            case ScheduledItemType.YouTubeItems:
                var youTubeItem = await youTubeItemManager.GetAsync(scheduledItem.ItemPrimaryKey, cancellationToken);
                if (youTubeItem is null)
                {
                    logger.LogWarning("YouTube item {ItemId} not found for scheduled item {ScheduledItemId}",
                        scheduledItem.ItemPrimaryKey, scheduledItem.Id);
                    return null;
                }
                return new SocialMediaPublishRequest
                {
                    Text = string.Empty,
                    Title = youTubeItem.Title,
                    LinkUrl = youTubeItem.Url,
                    ShortenedUrl = youTubeItem.ShortenedUrl,
                    Hashtags = youTubeItem.Tags.Count > 0 ? youTubeItem.Tags.ToList() : null,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerOid,
                };

            case ScheduledItemType.Engagements:
                var engagement = await engagementManager.GetAsync(scheduledItem.ItemPrimaryKey, cancellationToken);
                if (engagement is null)
                {
                    logger.LogWarning("Engagement {ItemId} not found for scheduled item {ScheduledItemId}",
                        scheduledItem.ItemPrimaryKey, scheduledItem.Id);
                    return null;
                }
                return new SocialMediaPublishRequest
                {
                    Text = string.Empty,
                    Title = engagement.Name,
                    LinkUrl = engagement.Url,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerOid,
                };

            case ScheduledItemType.Talks:
                var talk = await engagementManager.GetTalkAsync(scheduledItem.ItemPrimaryKey, cancellationToken);
                if (talk is null)
                {
                    logger.LogWarning("Talk {ItemId} not found for scheduled item {ScheduledItemId}",
                        scheduledItem.ItemPrimaryKey, scheduledItem.Id);
                    return null;
                }
                return new SocialMediaPublishRequest
                {
                    Text = string.Empty,
                    Title = talk.Name,
                    LinkUrl = talk.UrlForConferenceTalk,
                    ImageUrl = scheduledItem.ImageUrl,
                    OwnerEntraOid = ownerOid,
                };

            default:
                logger.LogError("The item type '{ItemType}' is not supported for scheduled item {ScheduledItemId}",
                    LogSanitizer.Sanitize(scheduledItem.ItemType.ToString()),
                    scheduledItem.Id);
                return null;
        }
    }

    private async Task PublishToQueueAsync(
        SocialMediaPublishRequest baseRequest,
        int platformId,
        string scheduledItemId,
        string ownerOid,
        CancellationToken cancellationToken)
    {
        if (!PlatformQueues.TryGetValue(platformId, out var queueName))
        {
            logger.LogWarning("Unknown SocialMediaPlatformId {PlatformId} for owner '{OwnerOid}' — skipping",
                platformId, LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var template = await messageTemplateManager.GetAsync(
            platformId,
            MessageTemplates.MessageTypes.ScheduledItem,
            ownerOid,
            cancellationToken);
        if (template is null)
        {
            logger.LogWarning("No message template for platform {PlatformId} / {EventType} for owner '{OwnerOid}' — skipping",
                platformId,
                MessageTemplates.MessageTypes.ScheduledItem,
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var composedText = await postComposer.ComposeAsync(baseRequest, template.Template, cancellationToken);
        if (string.IsNullOrWhiteSpace(composedText))
        {
            logger.LogWarning("Compose returned empty for platform {PlatformId} / scheduled item {ScheduledItemId} for owner '{OwnerOid}' — skipping",
                platformId,
                scheduledItemId,
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var publishRequest = new SocialMediaPublishRequest
        {
            Text = composedText,
            Title = baseRequest.Title,
            LinkUrl = baseRequest.LinkUrl,
            ShortenedUrl = baseRequest.ShortenedUrl,
            Hashtags = baseRequest.Hashtags,
            ImageUrl = baseRequest.ImageUrl,
            OwnerEntraOid = baseRequest.OwnerEntraOid,
        };

        var queueClient = queueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await queueClient.SendMessageAsync(JsonSerializer.Serialize(publishRequest), cancellationToken);

        logger.LogInformation(
            "Dispatched scheduled item {ScheduledItemId} to queue '{Queue}' for owner '{OwnerOid}'",
            scheduledItemId,
            queueName,
            LogSanitizer.Sanitize(ownerOid));
    }
}
