using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    IEngagementManager engagementManager,
    ISyndicationFeedItemManager SyndicationFeedItemManager,
    IYouTubeItemManager YouTubeItemManager,
    IUserOAuthTokenManager userOAuthTokenManager,
    ILinkedInManager linkedInManager,
    ILogger<ProcessScheduledItemFired> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInProcessScheduledItemFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink?> RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        logger.LogDebug("Started {FunctionName} at {StartedOn:f}",
            ConfigurationFunctionNames.LinkedInProcessScheduledItemFired, startedOn);

        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        try
        {
            var eventGridData = eventGridEvent.Data.ToString();
            var scheduledItemFiredEvent = JsonSerializer.Deserialize<ScheduledItemFiredEvent>(eventGridData);
            if (scheduledItemFiredEvent is null)
            {
                logger.LogError("Failed to parse the TableEvent data for event '{Id}'", eventGridEvent.Id);
                return null;
            }
            var scheduledItem = await scheduledItemManager.GetAsync(scheduledItemFiredEvent.Id);

            logger.LogDebug("Processing the event '{Id}' for '{TableName}', '{PartitionKey}'",
                eventGridEvent.Id, scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);

            // Resolve per-user OAuth token — no silent fallback to shared token
            var token = await userOAuthTokenManager.GetByUserAndPlatformAsync(
                scheduledItem.CreatedByEntraOid ?? string.Empty,
                SocialMediaPlatformIds.LinkedIn);

            if (token is null)
            {
                logger.LogWarning(
                    "No OAuth token found for owner {OwnerOid} on LinkedIn — skipping item {ItemId}",
                    LogSanitizer.Sanitize(scheduledItem.CreatedByEntraOid ?? string.Empty),
                    scheduledItem.Id);
                return null;
            }

            // Determine what type the post is for
            LinkedInPostLink linkedInPost;

            switch (scheduledItem.ItemType)
            {
                case ScheduledItemType.Engagements:
                    linkedInPost = await GetPostForEngagement(scheduledItem.ItemPrimaryKey);
                    break;
                case ScheduledItemType.Talks:
                    linkedInPost = await GetPostForTalk(scheduledItem.ItemPrimaryKey);
                    break;
                case ScheduledItemType.SyndicationFeedItems:
                    linkedInPost = await GetPostForSyndicationSource(scheduledItem.ItemPrimaryKey);
                    break;
                case ScheduledItemType.YouTubeItems:
                    linkedInPost = await GetPostForYouTubeItem(scheduledItem.ItemPrimaryKey);
                    break;
                default:
                    logger.LogError("The table name '{TableName}' is not supported", scheduledItem.ItemTableName);
                    return null;
            }

            linkedInPost.Text = await linkedInManager.ComposeMessageAsync(scheduledItem);
            linkedInPost.AccessToken = token.AccessToken;
            linkedInPost.ImageUrl = scheduledItem.ImageUrl;

            var properties = new Dictionary<string, string>
            {
                { "tableName", scheduledItem.ItemTableName },
                { "id", scheduledItem.ItemPrimaryKey.ToString() },
                { "text", linkedInPost.Text },
                { "url", linkedInPost.LinkUrl },
                { "title", linkedInPost.Title }
            };
            logger.LogCustomEvent(Metrics.LinkedInProcessedScheduledItemFired, properties);
            logger.LogDebug("Generated the LinkedIn post text for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);
            return linkedInPost;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new scheduled item. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
        finally
        {
            var endedOn = DateTimeOffset.Now;
            logger.LogDebug("Ended {FunctionName} at {EndedOn:f} with duration {Duration:c}",
                ConfigurationFunctionNames.LinkedInProcessScheduledItemFired, endedOn, endedOn - startedOn);
        }
    }

    private async Task<LinkedInPostLink> GetPostForEngagement(int primaryKey)
    {
        var post = new LinkedInPostLink();
        logger.LogDebug("Getting the text for engagement for '{PrimaryKey}'", primaryKey);
        try
        {
            var engagement = await engagementManager.GetAsync(primaryKey);
            post.Title = engagement.Name;
            post.LinkUrl = engagement.Url;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get the text for engagement for '{PrimaryKey}'", primaryKey);
            throw;
        }
        return post;
    }

    private async Task<LinkedInPostLink> GetPostForTalk(int primaryKey)
    {
        var post = new LinkedInPostLink();
        logger.LogDebug("Getting the text for Talk for '{PrimaryKey}'", primaryKey);
        try
        {
            var engagement = await engagementManager.GetTalkAsync(primaryKey);
            post.Title = engagement.Name;
            post.LinkUrl = engagement.UrlForConferenceTalk;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get the text for talk for '{PrimaryKey}'", primaryKey);
            throw;
        }
        return post;
    }

    private async Task<LinkedInPostLink> GetPostForSyndicationSource(int primaryKey)
    {
        var post = new LinkedInPostLink();
        logger.LogDebug("Getting the text for syndication source for '{PrimaryKey}'", primaryKey);
        try
        {
            var SyndicationFeedItem = await SyndicationFeedItemManager.GetAsync(primaryKey);
            post.Title = SyndicationFeedItem.Title;
            post.LinkUrl = SyndicationFeedItem.ShortenedUrl ?? SyndicationFeedItem.Url;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get the text for engagement for '{PrimaryKey}'", primaryKey);
            throw;
        }
        return post;
    }

    private async Task<LinkedInPostLink> GetPostForYouTubeItem(int primaryKey)
    {
        var post = new LinkedInPostLink();
        logger.LogDebug("Getting the text for YouTube source for '{PrimaryKey}'", primaryKey);
        try
        {
            var YouTubeItem = await YouTubeItemManager.GetAsync(primaryKey);
            post.Title = YouTubeItem.Title;
            post.LinkUrl = YouTubeItem.ShortenedUrl ?? YouTubeItem.Url;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get the text for engagement for '{PrimaryKey}'", primaryKey);
            throw;
        }
        return post;
    }

}
