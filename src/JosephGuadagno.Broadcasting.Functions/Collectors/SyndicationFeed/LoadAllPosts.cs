using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Extensions.Types;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;

public class LoadAllPosts(
    ISyndicationFeedReader syndicationFeedReader,
    IOptions<Settings> settingsOptions,
    ISyndicationFeedItemManager syndicationFeedItemManager,
    IFeedCheckManager feedCheckManager,
    IUrlShortener urlShortener,
    ILogger<LoadAllPosts> logger)
{
    [Function(ConfigurationFunctionNames.CollectorsFeedLoadAllPosts)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req,
        string checkFrom,
        string userOid)
    {
        var startedAt = DateTimeOffset.UtcNow;
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

        if (userOid.IsNullOrEmpty() || string.IsNullOrWhiteSpace(userOid))
        {
            const string message = "User OID is null or empty.  It is a required field.";
            logger.LogWarning(message);
            return new BadRequestObjectResult(message);
        }

        try
        {
            logger.LogDebug("Getting all items from feed from '{DateToCheckFrom}' for user '{UserOid}'", dateToCheckFrom, userOid);
            var newItems = await syndicationFeedReader.GetAsync(userOid, dateToCheckFrom);

            // If there is nothing new, save the last checked value and exit
            if (newItems.Count == 0)
            {
                logger.LogDebug("No posts found in the Json Feed");
                return new OkObjectResult("0 posts were found");
            }

            // Save the new items
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // Skip if item already exists for this user
                if (!await syndicationFeedItemManager.IsFeedItemUniqueToUser(item.FeedIdentifier, userOid))
                {
                    logger.LogDebug("Skipping duplicate syndication feed item with FeedIdentifier: '{FeedIdentifier}', for user '{UserOid}'", item.FeedIdentifier, userOid);
                    continue;
                }

                // shorten the url
                item.ShortenedUrl = await urlShortener.GetShortenedUrlAsync(item.Url, settingsOptions.Value.ShortenedDomainToUse);

                try
                {
                    var saveResult = await syndicationFeedItemManager.SaveAsync(item);
                    if (!saveResult.IsSuccess || saveResult.Value is null)
                    {
                        logger.LogError("Failed to save the blog post for user '{UserOId}' with the id of: '{Id}' Url:'{Url}'. Error: {Error}",
                            userOid, item.Id, item.Url, saveResult.ErrorMessage);
                        continue;
                    }
                    var savedItem = saveResult.Value;
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
                        "Failed to save the blog post for user '{UserOId}' with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                        userOid, item.Id, item.Url, e);
                }
            }

            // Save the last checked value
            var feedCheck =
                await feedCheckManager.GetByNameAsync(ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, userOid) ??
                new FeedCheck
                {
                    Name = ConfigurationFunctionNames.CollectorsFeedLoadNewPosts,
                    LastCheckedFeed = startedAt,
                    LastItemAddedOrUpdated = DateTimeOffset.Now,
                    EntraOId = userOid
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
