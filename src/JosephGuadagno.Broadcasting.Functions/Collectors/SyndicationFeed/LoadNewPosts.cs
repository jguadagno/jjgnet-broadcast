using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;

public class LoadNewPosts(
    ISyndicationFeedReader syndicationFeedReader,
    ISettings settings,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IFeedCheckManager feedCheckManager,
    IUrlShortener urlShortener,
    IEventPublisher eventPublisher,
    ILogger<LoadNewPosts> logger,
    TelemetryClient telemetryClient)
{

    [Function(ConfigurationFunctionNames.CollectorsFeedLoadNewPosts)]
    public async Task<IActionResult> RunAsync(
        [TimerTrigger("%collectors_feed_load_new_posts_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, startedAt);

        try
        {
            var feedCheck = await feedCheckManager.GetByNameAsync(
                                    ConfigurationFunctionNames.CollectorsFeedLoadNewPosts
                                ) ??
                                new FeedCheck { LastCheckedFeed = startedAt, LastItemAddedOrUpdated = DateTime.MinValue };

            // Check for new items
            logger.LogDebug("Checking the syndication feed for posts since '{LastItemAddedOrUpdated}'", feedCheck.LastItemAddedOrUpdated);
            var newItems = await syndicationFeedReader.GetAsync(feedCheck.LastItemAddedOrUpdated);

            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                feedCheck.LastCheckedFeed = startedAt;
                await feedCheckManager.SaveAsync(feedCheck);
                logger.LogDebug("No new or updated posts found in the syndication feed");
                return new OkObjectResult("0 speaking engagements were found");
            }

            // Save the new items to SyndicationFeedSource Repository
            var savedCount = 0;
            var eventsToPublish = new List<SyndicationFeedSource>();
            foreach (var item in newItems)
            {
                // shorten the url
                item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settings.BitlyShortenedDomain);

                // attempt to save the item
                try
                {
                    var savedItem = await syndicationFeedSourceManager.SaveAsync(item);
                    eventsToPublish.Add(savedItem);
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
                    return new BadRequestObjectResult($"Failed to save the blog post with the id of: '{item.Id}' Url:'{item.Url}'");
                }
            }

            // Publish the events
            var eventsPublished = await eventPublisher.PublishSyndicationFeedEventsAsync(
                ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, eventsToPublish);
            if (!eventsPublished)
            {
                logger.LogError("Failed to publish the events for the new or updated blog posts");
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
            logger.LogInformation("Loaded {SavedCount} of {TotalPostsCount} post(s)", savedCount, newItems.Count);
            return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} post(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load new posts. Exception: {ExceptionMessage}", exception.Message);
            return new BadRequestObjectResult($"Failed to load new posts. Exception: {exception.Message}");
        }
    }
}