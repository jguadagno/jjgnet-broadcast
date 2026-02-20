using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Extensions.Types;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;

public class LoadAllVideos(
    IYouTubeReader youTubeReader,
    ISettings settings,
    IYouTubeSourceManager youTubeSourceManager,
    IFeedCheckManager feedCheckManager,
    IUrlShortener urlShortener,
    ILogger<LoadAllVideos> logger,
    TelemetryClient telemetryClient)
{

    [Function(ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req, 
        string checkFrom)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos, startedAt);

        // Check for the date to check from
        var dateToCheckFrom = DateTime.MinValue;

        if (!checkFrom.IsNullOrEmpty())
        {
            var parsed = DateTime.TryParse(checkFrom, out var dateFrom);
            if (parsed)
            {
                dateToCheckFrom = dateFrom;
            }
        }

        try
        {
            logger.LogDebug("Getting all items from YouTube for the playlist since '{DateToCheckFrom}'", dateToCheckFrom);
            var newItems = await youTubeReader.GetAsync(dateToCheckFrom);

            // If there is nothing new, save the last checked value and exit
            if (newItems.Count == 0)
            {
                logger.LogInformation("No videos found in the playlist");
                return new OkObjectResult("0 videos were found");
            }

            // Save the new items
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // shorten the url
                item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settings.ShortenedDomainToUse);

                // attempt to save the item
                try
                {
                    var savedItem = await youTubeSourceManager.SaveAsync(item);

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

            // Save the last checked value
            var feedCheck =
                await feedCheckManager.GetByNameAsync(ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos) ??
                new FeedCheck
                {
                    Name = ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos,
                    LastCheckedFeed = startedAt,
                    LastItemAddedOrUpdated = DateTimeOffset.Now
                };
            var latestAdded = newItems.Max(item => item.PublicationDate);
            var latestUpdated = newItems.Max(item => item.LastUpdatedOn);
            feedCheck.LastItemAddedOrUpdated = latestUpdated > latestAdded
                ? latestUpdated
                : latestAdded;

            await feedCheckManager.SaveAsync(feedCheck);

            // Return
            logger.LogInformation("Loaded {SavedCount} of {TotalVideoCount} videos(s)", savedCount, newItems.Count);
            return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} videos(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load all videos. Exception: {ExceptionMessage}", exception.Message);
            return new BadRequestObjectResult(exception.Message);
        }
    }
}