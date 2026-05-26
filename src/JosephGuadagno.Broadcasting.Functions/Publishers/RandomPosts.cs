using System.Text.Json;
using Azure.Storage.Queues;
using Cronos;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class RandomPosts(
    IUserRandomPostSettingsDataStore userRandomPostSettingsDataStore,
    ISyndicationFeedItemManager syndicationFeedItemManager,
    IMessageTemplateManager messageTemplateManager,
    IPostComposer postComposer,
    QueueServiceClient queueServiceClient,
    ILogger<RandomPosts> logger)
{
    private static readonly Dictionary<int, string> PlatformQueues = new()
    {
        { SocialMediaPlatformIds.Twitter,  Queues.TwitterTweetsToSend },
        { SocialMediaPlatformIds.Bluesky,  Queues.BlueskyPostToSend },
        { SocialMediaPlatformIds.LinkedIn, Queues.LinkedInPostLink },
        { SocialMediaPlatformIds.Facebook, Queues.FacebookPostStatusToPage },
    };

    [Function(ConfigurationFunctionNames.PublishersRandomPosts)]
    public async Task RunAsync([TimerTrigger("0 * * * * *")] TimerInfo myTimer)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.PublishersRandomPosts, startedAt);

        var allSettings = await userRandomPostSettingsDataStore.GetAllActiveAsync();
        if (allSettings.Count == 0)
        {
            logger.LogDebug("No active per-user random post settings found");
            return;
        }

        var utcNow = DateTimeOffset.UtcNow;
        var currentMinute = new DateTimeOffset(
            utcNow.Year, utcNow.Month, utcNow.Day,
            utcNow.Hour, utcNow.Minute, 0, TimeSpan.Zero);
        var lastMinute = currentMinute.AddMinutes(-1);

        var settingsByOwner = allSettings
            .Where(s => !string.IsNullOrWhiteSpace(s.CreatedByEntraOid))
            .GroupBy(s => s.CreatedByEntraOid!)
            .ToList();

        foreach (var ownerGroup in settingsByOwner)
        {
            var ownerOid = ownerGroup.Key;
            foreach (var settings in ownerGroup)
            {
                if (!PlatformQueues.TryGetValue(settings.SocialMediaPlatformId, out var queueName))
                {
                    logger.LogWarning(
                        "Unknown SocialMediaPlatformId {PlatformId} for owner '{OwnerOid}' — skipping",
                        settings.SocialMediaPlatformId, LogSanitizer.Sanitize(ownerOid));
                    continue;
                }

                CronExpression cronExpression;
                try
                {
                    cronExpression = CronExpression.Parse(settings.CronExpression);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Invalid cron expression '{Cron}' for owner '{OwnerOid}' — skipping",
                        LogSanitizer.Sanitize(settings.CronExpression), LogSanitizer.Sanitize(ownerOid));
                    continue;
                }

                var nextOccurrence = cronExpression.GetNextOccurrence(lastMinute.UtcDateTime, TimeZoneInfo.Utc);
                if (nextOccurrence is null || nextOccurrence.Value > utcNow.UtcDateTime)
                {
                    continue;
                }

                var cutoffDate = settings.CutoffDate ?? DateTimeOffset.MinValue;
                var syndicationFeedItem = await syndicationFeedItemManager.GetRandomSyndicationDataAsync(
                    ownerOid, cutoffDate, settings.ExcludedCategories);

                if (syndicationFeedItem is null)
                {
                    logger.LogDebug(
                        "No random syndication item found for owner '{OwnerOid}' since '{CutoffDate:u}'",
                        LogSanitizer.Sanitize(ownerOid), cutoffDate);
                    continue;
                }

                var request = new SocialMediaPublishRequest
                {
                    Text = string.Empty,
                    Title = syndicationFeedItem.Title,
                    LinkUrl = syndicationFeedItem.Url,
                    ShortenedUrl = syndicationFeedItem.ShortenedUrl,
                    Hashtags = syndicationFeedItem.Tags.Count > 0 ? syndicationFeedItem.Tags.ToList() : null,
                    OwnerEntraOid = ownerOid,
                };

                var template = await messageTemplateManager.GetAsync(
                    settings.SocialMediaPlatformId, MessageTemplates.MessageTypes.RandomPost, ownerOid);
                if (template is null)
                {
                    logger.LogWarning(
                        "No message template found for platform {PlatformId} / {MessageType} for owner '{OwnerOid}'",
                        settings.SocialMediaPlatformId, MessageTemplates.MessageTypes.RandomPost,
                        LogSanitizer.Sanitize(ownerOid));
                    continue;
                }

                var composedText = await postComposer.ComposeAsync(request, template.Template);
                if (string.IsNullOrWhiteSpace(composedText))
                {
                    logger.LogWarning(
                        "Post composition returned empty for platform {PlatformId} / item {ItemId} for owner '{OwnerOid}'",
                        settings.SocialMediaPlatformId, syndicationFeedItem.Id,
                        LogSanitizer.Sanitize(ownerOid));
                    continue;
                }

                request.Text = composedText;

                var queueClient = queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(JsonSerializer.Serialize(request));

                logger.LogCustomEvent(Metrics.RandomPostFired, new Dictionary<string, string>
                {
                    { "title",    syndicationFeedItem.Title },
                    { "url",      syndicationFeedItem.Url },
                    { "id",       syndicationFeedItem.Id.ToString() },
                    { "platform", settings.SocialMediaPlatformId.ToString() },
                    { "owner",    LogSanitizer.Sanitize(ownerOid) },
                });

                logger.LogInformation(
                    "Dispatched random post '{Title}' (Id: {Id}) to queue '{Queue}' for owner '{OwnerOid}'",
                    syndicationFeedItem.Title, syndicationFeedItem.Id, queueName,
                    LogSanitizer.Sanitize(ownerOid));
            }
        }

        logger.LogDebug("{FunctionName} completed", ConfigurationFunctionNames.PublishersRandomPosts);
    }
}
