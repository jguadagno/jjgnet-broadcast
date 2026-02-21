using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Extensions.Types;

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;

public class LoadAllPosts(
    ISyndicationFeedReader syndicationFeedReader,
    ISettings settings,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IFeedCheckManager feedCheckManager,
    IUrlShortener urlShortener,
    ILogger<LoadAllPosts> logger,
    TelemetryClient telemetryClient)
{
    [Function(ConfigurationFunctionNames.CollectorsFeedLoadAllPosts)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req,
        string checkFrom)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsFeedLoadAllPosts, startedAt);

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
            logger.LogDebug("Getting all items from feed from '{DateToCheckFrom}'", dateToCheckFrom);
            var newItems = await syndicationFeedReader.GetAsync(dateToCheckFrom);

            // If there is nothing new, save the last checked value and exit
            if (newItems is null || newItems.Count == 0)
            {
                logger.LogDebug("No posts found in the Json Feed");
                return new OkObjectResult("0 posts were found");
            }

            // Save the new items
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // shorten the url
                item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settings.ShortenedDomainToUse);

                try
                {
                    var savedItem = await syndicationFeedSourceManager.SaveAsync(item);
                    telemetryClient.TrackEvent(Metrics.PostAddedOrUpdated,
                        new Dictionary<string, string>
                        {
                            { "Id", savedItem.Id.ToString() }, { "Url", savedItem.Url }, { "Title", savedItem.Title }
                        });
                    savedCount++;
                }
                catch (Exception e)
                {
                    logger.LogError(e,
                        "Failed to save the blog post with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                        item.Id, item.Url, e);
                }
            }

            // Save the last checked value
            var feedCheck =
                await feedCheckManager.GetByNameAsync(ConfigurationFunctionNames.CollectorsFeedLoadNewPosts) ??
                new FeedCheck
                {
                    Name = ConfigurationFunctionNames.CollectorsFeedLoadNewPosts,
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
            logger.LogInformation("Loaded {SavedCount} of {TotalPostsCount} post(s)", savedCount, newItems.Count);
            return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} post(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load all posts. Exception: {ExceptionMessage}", exception.Message);
            return new BadRequestObjectResult(exception.Message);
        }
    }
}