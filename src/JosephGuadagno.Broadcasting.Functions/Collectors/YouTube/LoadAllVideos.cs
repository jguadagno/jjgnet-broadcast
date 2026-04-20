using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Extensions.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;

public class LoadAllVideos(
    IYouTubeReader youTubeReader,
    IOptions<Settings> settingsOptions,
    IYouTubeSourceManager youTubeSourceManager,
    IFeedCheckManager feedCheckManager,
    IUrlShortener urlShortener,
    ILogger<LoadAllVideos> logger)
{

    [Function(ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req, 
        string checkFrom)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos, startedAt);

        // Check for the date to check from
        var dateToCheckFrom = DateTimeOffset.MinValue;

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
            var ownerOid = await CollectorOwnerOidResolver.ResolveAsync(
                youTubeSourceManager,
                logger,
                ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos);
            if (string.IsNullOrWhiteSpace(ownerOid))
            {
                return new BadRequestObjectResult("Unable to resolve collector owner OID from YouTube source records.");
            }

            logger.LogDebug("Getting all items from YouTube for the playlist since '{DateToCheckFrom}'", dateToCheckFrom);
            var newItems = await youTubeReader.GetAsync(ownerOid, dateToCheckFrom);

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
                // Skip if item already exists
                var existingItem = await youTubeSourceManager.GetByVideoIdAsync(item.VideoId);
                if (existingItem != null)
                {
                    logger.LogDebug("Skipping duplicate YouTube video with VideoId: '{VideoId}'", item.VideoId);
                    continue;
                }

                // shorten the url
                item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settingsOptions.Value.ShortenedDomainToUse);

                // attempt to save the item
                try
                {
                    var saveResult = await youTubeSourceManager.SaveAsync(item);

                    if (!saveResult.IsSuccess || saveResult.Value is null)
                    {
                        logger.LogError("Failed to save the video with the VideoId of: '{VideoId}' Url:'{Url}'. Error: {Error}",
                            item.VideoId, item.Url, saveResult.ErrorMessage);
                        return new BadRequestObjectResult($"Failed to save the video with the id of: '{item.VideoId}' Url:'{item.Url}'");
                    }
                    var savedItem = saveResult.Value;
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
