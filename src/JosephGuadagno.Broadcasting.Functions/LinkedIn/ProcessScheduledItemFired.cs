using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    IEngagementManager engagementManager,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IYouTubeSourceManager youTubeSourceManager,
    ILinkedInApplicationSettings linkedInApplicationSettings,
    ITelemetryClient telemetryClient,
    ILogger<ProcessScheduledItemFired> logger)
{

    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=linkedin_process_scheduled_item_fired`
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

            // Determine what type the post is for
            LinkedInPostLink linkedInPost;
            // The scheduled post should always have a message.  We just need to craft the Title and Urls

            switch (scheduledItem.ItemTableName)
            {
                case SourceSystems.Engagements:
                    linkedInPost = await GetPostForEngagement(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.Talks:
                    linkedInPost = await GetPostForTalk(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.SyndicationFeedSources:
                    linkedInPost = await GetPostForSyndicationSource(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.YouTubeSources:
                    linkedInPost = await GetPostForYouTubeSource(scheduledItem.ItemPrimaryKey);
                    break;
                default:
                    logger.LogError("The table name '{TableName}' is not supported", scheduledItem.ItemTableName);
                    return null;
            }

            linkedInPost.Text = scheduledItem.Message;
            linkedInPost.AuthorId = linkedInApplicationSettings.AuthorId;
            linkedInPost.AccessToken = linkedInApplicationSettings.AccessToken;

            telemetryClient.TrackEvent(Metrics.LinkedInProcessedScheduledItemFired,
                new Dictionary<string, string>
                {
                    { "tableName", scheduledItem.ItemTableName },
                    { "id", scheduledItem.ItemPrimaryKey.ToString() },
                    { "text", linkedInPost.Text },
                    { "url", linkedInPost.LinkUrl },
                    { "title", linkedInPost.Title }
                });
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
            // Generate the title and Url for the LinkedIn post
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
            // Generate the title and Url for the LinkedIn post
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
            var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(primaryKey);
            // Generate the title and Url for the LinkedIn post
            post.Title = syndicationFeedSource.Title;
            post.LinkUrl = syndicationFeedSource.ShortenedUrl ?? syndicationFeedSource.Url;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get the text for engagement for '{PrimaryKey}'", primaryKey);
            throw;
        }

        return post;
    }

    private async Task<LinkedInPostLink> GetPostForYouTubeSource(int primaryKey)
    {
        var post = new LinkedInPostLink();

        logger.LogDebug("Getting the text for YouTube source for '{PrimaryKey}'", primaryKey);
        try
        {
            var youTubeSource = await youTubeSourceManager.GetAsync(primaryKey);
            // Generate the title and Url for the LinkedIn post
            post.Title = youTubeSource.Title;
            post.LinkUrl = youTubeSource.ShortenedUrl ?? youTubeSource.Url;

        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get the text for engagement for '{PrimaryKey}'", primaryKey);
            throw;
        }

        return post;
    }
}