using System.Text.Json;
using Azure.Storage.Queues;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Services;

public class CollectorEventDistributor(
    IUserEventDistributorMappingDataStore userEventDistributorMappingDataStore,
    IMessageTemplateManager messageTemplateManager,
    IPostComposer postComposer,
    QueueServiceClient queueServiceClient,
    ILogger<CollectorEventDistributor> logger) : ICollectorEventDistributor
{
    private static readonly Dictionary<int, string> PlatformQueues = new()
    {
        { SocialMediaPlatformIds.Twitter,  Queues.TwitterTweetsToSend },
        { SocialMediaPlatformIds.Bluesky,  Queues.BlueskyPostToSend },
        { SocialMediaPlatformIds.LinkedIn, Queues.LinkedInPostLink },
        { SocialMediaPlatformIds.Facebook, Queues.FacebookPostStatusToPage },
    };

    public async Task DispatchSyndicationFeedItemAsync(SyndicationFeedItem item, string ownerOid, CancellationToken cancellationToken = default)
    {
        var mappings = await userEventDistributorMappingDataStore.GetByUserAndEventTypeAsync(
            ownerOid, MessageTemplates.MessageTypes.NewSyndicationFeedItem, cancellationToken);

        if (mappings.Count == 0)
        {
            logger.LogDebug("No active distributor mappings for owner '{OwnerOid}' / {EventType}",
                LogSanitizer.Sanitize(ownerOid), MessageTemplates.MessageTypes.NewSyndicationFeedItem);
            return;
        }

        var request = new SocialMediaPublishRequest
        {
            Text = string.Empty,
            Title = item.Title,
            LinkUrl = item.Url,
            ShortenedUrl = item.ShortenedUrl,
            Hashtags = item.Tags.Count > 0 ? item.Tags.ToList() : null,
            OwnerEntraOid = ownerOid,
        };

        foreach (var mapping in mappings)
        {
            await PublishToQueueAsync(request, mapping.SocialMediaPlatformId,
                MessageTemplates.MessageTypes.NewSyndicationFeedItem, item.Id.ToString(), ownerOid, cancellationToken);
        }
    }

    public async Task DispatchYouTubeItemAsync(YouTubeItem item, string ownerOid, CancellationToken cancellationToken = default)
    {
        var mappings = await userEventDistributorMappingDataStore.GetByUserAndEventTypeAsync(
            ownerOid, MessageTemplates.MessageTypes.NewYouTubeItem, cancellationToken);

        if (mappings.Count == 0)
        {
            logger.LogDebug("No active distributor mappings for owner '{OwnerOid}' / {EventType}",
                LogSanitizer.Sanitize(ownerOid), MessageTemplates.MessageTypes.NewYouTubeItem);
            return;
        }

        var request = new SocialMediaPublishRequest
        {
            Text = string.Empty,
            Title = item.Title,
            LinkUrl = item.Url,
            ShortenedUrl = item.ShortenedUrl,
            Hashtags = item.Tags.Count > 0 ? item.Tags.ToList() : null,
            OwnerEntraOid = ownerOid,
        };

        foreach (var mapping in mappings)
        {
            await PublishToQueueAsync(request, mapping.SocialMediaPlatformId,
                MessageTemplates.MessageTypes.NewYouTubeItem, item.Id.ToString(), ownerOid, cancellationToken);
        }
    }

    public async Task DispatchSpeakingEngagementAsync(Engagement item, string ownerOid, CancellationToken cancellationToken = default)
    {
        var mappings = await userEventDistributorMappingDataStore.GetByUserAndEventTypeAsync(
            ownerOid, MessageTemplates.MessageTypes.NewSpeakingEngagement, cancellationToken);

        if (mappings.Count == 0)
        {
            logger.LogDebug("No active distributor mappings for owner '{OwnerOid}' / {EventType}",
                LogSanitizer.Sanitize(ownerOid), MessageTemplates.MessageTypes.NewSpeakingEngagement);
            return;
        }

        var request = new SocialMediaPublishRequest
        {
            Text = string.Empty,
            Title = item.Name,
            LinkUrl = item.Url,
            OwnerEntraOid = ownerOid,
        };

        foreach (var mapping in mappings)
        {
            await PublishToQueueAsync(request, mapping.SocialMediaPlatformId,
                MessageTemplates.MessageTypes.NewSpeakingEngagement, item.Id.ToString(), ownerOid, cancellationToken);
        }
    }

    private async Task PublishToQueueAsync(
        SocialMediaPublishRequest baseRequest,
        int platformId,
        string eventType,
        string itemId,
        string ownerOid,
        CancellationToken cancellationToken)
    {
        if (!PlatformQueues.TryGetValue(platformId, out var queueName))
        {
            logger.LogWarning("Unknown SocialMediaPlatformId {PlatformId} for owner '{OwnerOid}' — skipping",
                platformId, LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var template = await messageTemplateManager.GetAsync(platformId, eventType, ownerOid, cancellationToken);
        if (template is null)
        {
            logger.LogWarning("No message template for platform {PlatformId} / {EventType} for owner '{OwnerOid}' — skipping",
                platformId, eventType, LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var composedText = await postComposer.ComposeAsync(baseRequest, template.Template, cancellationToken);
        if (string.IsNullOrWhiteSpace(composedText))
        {
            logger.LogWarning("Compose returned empty for platform {PlatformId} / item {ItemId} for owner '{OwnerOid}' — skipping",
                platformId, itemId, LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var publishRequest = new SocialMediaPublishRequest
        {
            Text = composedText,
            Title = baseRequest.Title,
            LinkUrl = baseRequest.LinkUrl,
            ShortenedUrl = baseRequest.ShortenedUrl,
            Hashtags = baseRequest.Hashtags,
            OwnerEntraOid = baseRequest.OwnerEntraOid,
        };

        var queueClient = queueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await queueClient.SendMessageAsync(JsonSerializer.Serialize(publishRequest), cancellationToken);

        logger.LogInformation(
            "Dispatched {EventType} item {ItemId} to queue '{Queue}' for owner '{OwnerOid}'",
            eventType, itemId, queueName, LogSanitizer.Sanitize(ownerOid));
    }
}
