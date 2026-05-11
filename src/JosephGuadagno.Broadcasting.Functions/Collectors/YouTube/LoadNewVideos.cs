using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;

public class LoadNewVideos(
    IYouTubeReader youTubeReader,
    IOptions<Settings> settingsOptions,
    IFeedCheckManager feedCheckManager,
    IUserCollectorYouTubeChannelManager userCollectorYouTubeChannelManager,
    IYouTubeItemManager YouTubeItemManager,
    IUrlShortener urlShortener,
    IEventPublisher eventPublisher,
    ILogger<LoadNewVideos> logger)
{
    private static readonly ResiliencePipeline SavePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential
        })
        .Build();

    [Function(ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos)]
    public async Task<IActionResult> RunAsync(
        [TimerTrigger("%collectors_youtube_load_new_videos_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos, startedAt);

        try
        {
            var configs = await userCollectorYouTubeChannelManager.GetAllActiveAsync();
            if (configs.Count == 0)
            {
                logger.LogDebug("No active YouTube channel configurations found");
                return new OkObjectResult("No active YouTube channel configurations found");
            }

            var totalSavedCount = 0;
            var totalFoundCount = 0;

            foreach (var config in configs)
            {
                var feedCheck = await feedCheckManager.GetByNameAsync(
                    ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos, config.CreatedByEntraOid
                ) ?? new FeedCheck
                {
                    Name = ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos,
                    LastCheckedFeed = startedAt,
                    LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                    EntraOId = config.CreatedByEntraOid
                };

                logger.LogDebug("Checking playlist for videos for owner '{OwnerOid}' since '{LastItemAddedOrUpdated}'",
                    config.CreatedByEntraOid, feedCheck.LastItemAddedOrUpdated);

                var newItems = await youTubeReader.GetAsync(config.CreatedByEntraOid, feedCheck.LastItemAddedOrUpdated);

                if (newItems.Count == 0)
                {
                    feedCheck.LastCheckedFeed = startedAt;
                    await feedCheckManager.SaveAsync(feedCheck);
                    logger.LogDebug("No new videos for owner '{OwnerOid}'", config.CreatedByEntraOid);
                    continue;
                }

                totalFoundCount += newItems.Count;

                var savedCount = 0;
                var eventsToPublish = new List<YouTubeItem>();
                foreach (var item in newItems)
                {
                    var existingItem = await YouTubeItemManager.GetByVideoIdAsync(item.VideoId);
                    if (existingItem != null)
                    {
                        logger.LogDebug("Skipping duplicate YouTube video with VideoId: '{VideoId}'", item.VideoId);
                        continue;
                    }

                    item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settingsOptions.Value.ShortenedDomainToUse);

                    try
                    {
                        var saveResult = await SavePipeline.ExecuteAsync(
                            async ct => await YouTubeItemManager.SaveAsync(item));

                        if (!saveResult.IsSuccess || saveResult.Value is null)
                        {
                            logger.LogError("Failed to save the video with the VideoId of: '{VideoId}' Url:'{Url}'. Error: {Error}",
                                item.VideoId, item.Url, saveResult.ErrorMessage);
                            continue;
                        }
                        var savedItem = saveResult.Value;
                        eventsToPublish.Add(savedItem);
                        var properties = new Dictionary<string, string>
                        {
                            { "Id", savedItem.Id.ToString() }, { "VideoId", savedItem.VideoId },
                            { "Url", savedItem.Url }, { "Title", savedItem.Title }
                        };
                        logger.LogCustomEvent(Metrics.VideoAddedOrUpdated, properties);
                        savedCount++;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e,
                            "Failed to save the video with the VideoId of: '{VideoId}' Url:'{Url}'. Exception: {ExceptionMessage}",
                            item.VideoId, item.Url, e);
                        continue;
                    }
                }

                await eventPublisher.PublishYouTubeEventsAsync(
                    ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos,
                    eventsToPublish);

                feedCheck.LastCheckedFeed = startedAt;
                feedCheck.LastUpdatedOn = DateTimeOffset.UtcNow;
                feedCheck.EntraOId = config.CreatedByEntraOid;
                var latestAdded = newItems.Max(item => item.PublicationDate);
                var latestUpdated = newItems.Max(item => item.LastUpdatedOn);
                feedCheck.LastItemAddedOrUpdated = latestUpdated > latestAdded
                    ? latestUpdated
                    : latestAdded;

                await feedCheckManager.SaveAsync(feedCheck);

                totalSavedCount += savedCount;
                logger.LogInformation("Loaded {SavedCount} of {TotalVideoCount} video(s) for owner '{OwnerOid}'",
                    savedCount, newItems.Count, config.CreatedByEntraOid);
            }

            return new OkObjectResult($"Loaded {totalSavedCount} of {totalFoundCount} video(s) across {configs.Count} channel(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while loading new YouTube videos");
            return new BadRequestObjectResult($"An error occurred while loading new YouTube videos: {exception.Message}");
        }
    }
}
