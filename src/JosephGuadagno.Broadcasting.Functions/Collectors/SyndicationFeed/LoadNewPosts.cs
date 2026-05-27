using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.Functions.Services;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;

public class LoadNewPosts(
    ISyndicationFeedReader syndicationFeedReader,
    IOptions<Settings> settingsOptions,
    ISyndicationFeedItemManager syndicationFeedItemManager,
    IUserCollectorFeedSourceManager userCollectorFeedSourceManager,
    IFeedCheckManager feedCheckManager,
    IUrlShortener urlShortener,
    ICollectorEventDispatcher collectorEventDispatcher,
    ILogger<LoadNewPosts> logger)
{
    private static readonly ResiliencePipeline SavePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential
        })
        .Build();

    [Function(ConfigurationFunctionNames.CollectorsFeedLoadNewPosts)]
    public async Task<IActionResult> RunAsync(
        [TimerTrigger("%collectors_feed_load_new_posts_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, startedAt);

        try
        {
            var configs = await userCollectorFeedSourceManager.GetAllActiveAsync();
            if (configs.Count == 0)
            {
                logger.LogDebug("No active feed source configurations found");
                return new OkObjectResult("No active feed source configurations found");
            }

            var totalSavedCount = 0;
            var totalFoundCount = 0;

            foreach (var config in configs)
            {
                var feedCheck = await feedCheckManager.GetByNameAsync(
                    ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, config.CreatedByEntraOid
                ) ?? new FeedCheck
                {
                    Name = ConfigurationFunctionNames.CollectorsFeedLoadNewPosts,
                    LastCheckedFeed = startedAt,
                    LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                    EntraOId = config.CreatedByEntraOid
                };

                logger.LogDebug("Checking feed '{FeedUrl}' for owner '{OwnerOid}' since '{LastItemAddedOrUpdated}'",
                    config.FeedUrl, config.CreatedByEntraOid, feedCheck.LastItemAddedOrUpdated);

                var newItems = await syndicationFeedReader.GetAsync(config.FeedUrl, config.CreatedByEntraOid, feedCheck.LastItemAddedOrUpdated);

                if (newItems.Count == 0)
                {
                    feedCheck.LastCheckedFeed = startedAt;
                    await feedCheckManager.SaveAsync(feedCheck);
                    logger.LogDebug("No new posts for owner '{OwnerOid}'", config.CreatedByEntraOid);
                    continue;
                }

                totalFoundCount += newItems.Count;

                var savedCount = 0;
                var eventsToPublish = new List<SyndicationFeedItem>();
                foreach (var item in newItems)
                {
                    if (!await syndicationFeedItemManager.IsFeedItemUniqueToUser(item.FeedIdentifier, config.CreatedByEntraOid))
                    {
                        logger.LogDebug("Skipping duplicate syndication feed item with FeedIdentifier: '{FeedIdentifier}'", item.FeedIdentifier);
                        continue;
                    }

                    item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settingsOptions.Value.ShortenedDomainToUse);

                    try
                    {
                        var saveResult = await SavePipeline.ExecuteAsync(
                            async ct => await syndicationFeedItemManager.SaveAsync(item, ct));

                        if (!saveResult.IsSuccess || saveResult.Value is null)
                        {
                            logger.LogError("Failed to save the blog post with the id of: '{Id}' Url:'{Url}'. Error: {Error}",
                                item.Id, item.Url, saveResult.ErrorMessage);
                            continue;
                        }
                        var savedItem = saveResult.Value;
                        eventsToPublish.Add(savedItem);

                        var properties = new Dictionary<string, string>
                            {
                                { "Id", savedItem.Id.ToString() }, { "Url", savedItem.Url }, { "Title", savedItem.Title }
                            };
                        logger.LogCustomEvent(Metrics.PostAddedOrUpdated, properties);
                        savedCount++;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e,
                            "Failed to save the blog post with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                            item.Id, item.Url, e);
                    }
                }

                foreach (var savedItem in eventsToPublish)
                {
                    await collectorEventDispatcher.DispatchSyndicationFeedItemAsync(savedItem, config.CreatedByEntraOid);
                }

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
                logger.LogInformation("Loaded {SavedCount} of {TotalPostsCount} post(s) for owner '{OwnerOid}'",
                    savedCount, newItems.Count, config.CreatedByEntraOid);
            }

            return new OkObjectResult($"Loaded {totalSavedCount} of {totalFoundCount} post(s) across {configs.Count} feed source(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load new posts. Exception: {ExceptionMessage}", exception.Message);
            return new BadRequestObjectResult($"Failed to load new posts. Exception: {exception.Message}");
        }
    }
}
