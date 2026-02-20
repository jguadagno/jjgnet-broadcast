using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;

public class LoadNewVideos(
    IYouTubeReader youTubeReader,
    ISettings settings,
    IFeedCheckManager feedCheckManager,
    IYouTubeSourceManager youTubeSourceManager,
    IUrlShortener urlShortener,
    IEventPublisher eventPublisher,
    ILogger<LoadNewVideos> logger,
    TelemetryClient telemetryClient)
{

    [Function(ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos)]
    public async Task<IActionResult> RunAsync(
        [TimerTrigger("%collectors_youtube_load_new_videos_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos, startedAt);

        try
        {
            var feedCheck = await feedCheckManager.GetByNameAsync(
                                ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos
                            ) ??
                            new FeedCheck { LastCheckedFeed = startedAt, LastItemAddedOrUpdated = DateTime.MinValue };

            // Check for new items
            logger.LogDebug("Checking playlist for videos since '{LastItemAddedOrUpdated}'",
                feedCheck.LastItemAddedOrUpdated);
            var newItems = await youTubeReader.GetAsync(feedCheck.LastItemAddedOrUpdated);

            // If there is nothing new, save the last checked value and exit
            if (newItems.Count == 0)
            {
                feedCheck.LastCheckedFeed = startedAt;
                await feedCheckManager.SaveAsync(feedCheck);
                logger.LogDebug("No new videos found in the playlist");
                return new OkObjectResult("0 videos were found");
            }

            // Save the new items to SourceDataRepository
            var savedCount = 0;
            var eventsToPublish = new List<YouTubeSource>();
            foreach (var item in newItems)
            {
                // shorten the url
                item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settings.ShortenedDomainToUse);

                // attempt to save the item
                try
                {
                    var savedItem = await youTubeSourceManager.SaveAsync(item);

                    eventsToPublish.Add(savedItem);
                    telemetryClient.TrackEvent(Metrics.VideoAddedOrUpdated,
                        new Dictionary<string, string>
                        {
                            { "Id", savedItem.Id.ToString() }, { "VideoId", savedItem.VideoId },
                            { "Url", savedItem.Url }, { "Title", savedItem.Title }
                        });
                    savedCount++;

                }
                catch (Exception e)
                {
                    logger.LogError(e,
                        "Failed to save the video with the VideoId of: '{VideoId}' Url:'{Url}'. Exception: {ExceptionMessage}",
                        item.VideoId, item.Url, e);
                    return new BadRequestObjectResult($"Failed to save the video with the id of: '{item.VideoId}' Url:'{item.Url}'");
                }
            }

            // Publish the events
            var eventsPublished = await eventPublisher.PublishYouTubeEventsAsync(
                ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos,
                eventsToPublish);
            if (!eventsPublished)
            {
                logger.LogError("Failed to publish the events for the new or updated videos");
            }

            // Save the last checked value
            feedCheck.LastCheckedFeed = startedAt;
            feedCheck.LastUpdatedOn = DateTime.UtcNow;
            var latestAdded = newItems.Max(item => item.PublicationDate);
            var latestUpdated = newItems.Max(item => item.LastUpdatedOn);
            feedCheck.LastItemAddedOrUpdated = latestUpdated > latestAdded
                ? latestUpdated
                : latestAdded;

            await feedCheckManager.SaveAsync(feedCheck);

            // Return
            logger.LogInformation("Loaded {SavedCount} of {TotalVideoCount} video(s)", savedCount, newItems.Count);
            return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} video(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while loading new YouTube videos");
            return new BadRequestObjectResult($"An error occurred while loading new YouTube videos: {exception.Message}");
        }
    }
}