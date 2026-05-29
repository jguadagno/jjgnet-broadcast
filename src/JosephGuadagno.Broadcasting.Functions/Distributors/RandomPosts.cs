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

namespace JosephGuadagno.Broadcasting.Functions.Distributors;

public class RandomPosts(
    IUserRandomPostSettingsManager userRandomPostSettingsManager,
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

    [Function(ConfigurationFunctionNames.DistributorsRandomPosts)]
    public async Task RunAsync([TimerTrigger("%dispatchers_random_post_cron_settings%")] TimerInfo myTimer)
    {
        var utcNow = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.DistributorsRandomPosts, utcNow);

        var dueSettings = await userRandomPostSettingsManager.GetAllDueAsync(utcNow);
        if (dueSettings.Count == 0)
        {
            logger.LogDebug("No due per-user random post settings found");
            return;
        }

        foreach (var settings in dueSettings)
        {
            var ownerOid = settings.CreatedByEntraOid;

            if (!PlatformQueues.TryGetValue(settings.SocialMediaPlatformId, out var queueName))
            {
                logger.LogWarning(
                    "Unknown SocialMediaPlatformId {PlatformId} for owner '{OwnerOid}' — skipping",
                    settings.SocialMediaPlatformId, LogSanitizer.Sanitize(ownerOid));
                await AdvanceNextRunAsync(settings, utcNow);
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

                var deactivated = await userRandomPostSettingsManager.IncrementCronFailureAsync(settings.Id);
                if (deactivated)
                {
                    logger.LogCustomEvent(Metrics.RandomPostCronCircuitBroken, new Dictionary<string, string>
                    {
                        { "settingsId", settings.Id.ToString() },
                        { "cron", LogSanitizer.Sanitize(settings.CronExpression) },
                        { "owner", LogSanitizer.Sanitize(ownerOid) },
                        { "platform", settings.SocialMediaPlatformId.ToString() },
                    });

                    logger.LogWarning(
                        "Random post settings ID {Id} deactivated after 5 consecutive cron parse failures for owner '{OwnerOid}' — cron: '{Cron}'",
                        settings.Id,
                        LogSanitizer.Sanitize(ownerOid),
                        LogSanitizer.Sanitize(settings.CronExpression));
                }

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
                await AdvanceNextRunAsync(settings, cronExpression);
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
                await AdvanceNextRunAsync(settings, cronExpression);
                continue;
            }

            var composedText = await postComposer.ComposeAsync(request, template.Template);
            if (string.IsNullOrWhiteSpace(composedText))
            {
                logger.LogWarning(
                    "Post composition returned empty for platform {PlatformId} / item {ItemId} for owner '{OwnerOid}'",
                    settings.SocialMediaPlatformId, syndicationFeedItem.Id,
                    LogSanitizer.Sanitize(ownerOid));
                await AdvanceNextRunAsync(settings, cronExpression);
                continue;
            }

            request.Text = composedText;

            try
            {
                var queueClient = queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(JsonSerializer.Serialize(request));

                logger.LogCustomEvent(Metrics.RandomPostFired, new Dictionary<string, string>
                {
                    { "title",    LogSanitizer.Sanitize(syndicationFeedItem.Title) },
                    { "url",      syndicationFeedItem.Url },
                    { "id",       syndicationFeedItem.Id.ToString() },
                    { "platform", settings.SocialMediaPlatformId.ToString() },
                    { "owner",    LogSanitizer.Sanitize(ownerOid) },
                });

                logger.LogInformation(
                    "Dispatched random post '{Title}' (Id: {Id}) to queue '{Queue}' for owner '{OwnerOid}'",
                    LogSanitizer.Sanitize(syndicationFeedItem.Title), syndicationFeedItem.Id, queueName,
                    LogSanitizer.Sanitize(ownerOid));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to dispatch random post to queue '{Queue}' for owner '{OwnerOid}'",
                    queueName, LogSanitizer.Sanitize(ownerOid));
            }

            // Advance NextRunDateUtc regardless of dispatch outcome to prevent immediate retry.
            await AdvanceNextRunAsync(settings, cronExpression);
        }

        logger.LogDebug("{FunctionName} completed", ConfigurationFunctionNames.DistributorsRandomPosts);
    }

    private async Task AdvanceNextRunAsync(UserRandomPostSettings settings, CronExpression cronExpression)
    {
        var nextRun = cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
        await userRandomPostSettingsManager.UpdateNextRunAsync(settings.Id, nextRun);
    }

    private async Task AdvanceNextRunAsync(UserRandomPostSettings settings, DateTimeOffset utcNow)
    {
        try
        {
            var cronExpression = CronExpression.Parse(settings.CronExpression);
            var nextRun = cronExpression.GetNextOccurrence(utcNow, TimeZoneInfo.Utc);
            await userRandomPostSettingsManager.UpdateNextRunAsync(settings.Id, nextRun);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not compute next run for settings ID {Id} with cron '{Cron}'",
                settings.Id, LogSanitizer.Sanitize(settings.CronExpression));
        }
    }
}
