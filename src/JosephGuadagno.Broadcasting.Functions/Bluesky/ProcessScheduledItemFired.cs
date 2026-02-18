using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;

using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IYouTubeSourceManager youTubeSourceManager,
    IEngagementManager engagementManager,
    TelemetryClient telemetryClient,
    ILogger<ProcessScheduledItemFired> logger)
{
    const int MaxPostLength = 300;

    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_scheduled_item_fired`
    [Function(ConfigurationFunctionNames.BlueskyProcessScheduledItemFired)]
    [QueueOutput(Queues.BlueskyPostToSend)]
    public async Task<string?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        logger.LogDebug("{FunctionName} started at: {StartedOn:f}",
            ConfigurationFunctionNames.BlueskyProcessScheduledItemFired, startedOn);

        // Get the Source Data identifier for the event
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
            string blueSkyPostText;
            // The scheduled post should always have a message.  We just need to craft the Title and Urls

            switch (scheduledItem.ItemTableName)
            {
                case SourceSystems.Engagements:
                    blueSkyPostText = await GetPostForEngagement(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.Talks:
                    blueSkyPostText = await GetPostForTalk(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.SyndicationFeedSources:
                    blueSkyPostText = await GetPostForSyndicationSource(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.YouTubeSources:
                    blueSkyPostText = await GetPostForYouTubeSource(scheduledItem.ItemPrimaryKey);
                    break;
                default:
                    logger.LogError("The table name '{TableName}' is not supported", scheduledItem.ItemTableName);
                    return null;
            }

            telemetryClient.TrackEvent(Metrics.BlueskyProcessedScheduledItemFired,
                new Dictionary<string, string>
                {
                    { "tableName", scheduledItem.ItemTableName },
                    { "id", scheduledItem.ItemPrimaryKey.ToString() },
                    { "text", blueSkyPostText }
                });
            logger.LogDebug("Generated the BlueSky post text for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);
            return blueSkyPostText;
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
                ConfigurationFunctionNames.BlueskyProcessScheduledItemFired, endedOn, endedOn - startedOn);
        }
    }

    private async Task<string> GetPostForSyndicationSource(int primaryKey)
    {

        var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(primaryKey);

        var statusText = "Blog Post: ";
        var url = syndicationFeedSource.ShortenedUrl ?? syndicationFeedSource.Url;
        var postTitle = syndicationFeedSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(syndicationFeedSource.Tags);

        if (statusText.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= MaxPostLength)
        {
            var newLength = MaxPostLength - statusText.Length - url.Length - hashTagList.Length - 1;
            postTitle = string.Concat(postTitle.AsSpan(0, newLength - 4), "...");
        }

        var blueskyPost = $"{statusText} {postTitle} {url} {hashTagList}";

        logger.LogDebug("Composed Bluesky Post '{BlueskyPost}'", blueskyPost);
        return blueskyPost;
    }

    private async Task<string> GetPostForYouTubeSource(int primaryKey)
    {

        var youTubeSource = await youTubeSourceManager.GetAsync(primaryKey);

        var statusText = "Video: ";
        var url = youTubeSource.ShortenedUrl ?? youTubeSource.Url;
        var postTitle = youTubeSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(youTubeSource.Tags);

        if (statusText.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= MaxPostLength)
        {
            var newLength = MaxPostLength - statusText.Length - url.Length - hashTagList.Length - 1;
            postTitle = string.Concat(postTitle.AsSpan(0, newLength - 4), "...");
        }

        var blueskyPost = $"{statusText} {postTitle} {url} {hashTagList}";

        logger.LogDebug("Composed Bluesky Post '{BlueskyPost}'", blueskyPost);
        return blueskyPost;
    }


    private async Task<string> GetPostForEngagement(int primaryKey)
    {
        // TODO: Account for custom images for engagement
        // TODO: Account for custom message for engagement
        //  i.e: Join me tomorrow, Join me next week
        // TODO: Maybe handle timezone?

        var engagement = await engagementManager.GetAsync(primaryKey);

        var statusText = $"I'm speaking at {engagement.Name} ({engagement.Url}) starting on {engagement.StartDateTime:f}";
        var comments = engagement.Comments;
        var commentsLength = comments?.Length ?? 0;
        statusText += " " + comments;

        if (statusText.Length + commentsLength + 1 >= MaxPostLength)
        {
            var newLength = MaxPostLength - statusText.Length - commentsLength - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }

        logger.LogDebug("Composed BlueskyPost '{StatusText}'", statusText);
        return statusText;
    }

    private async Task<string> GetPostForTalk(int primaryKey)
    {

        // TODO: Account for custom images for talk
        // TODO: Account for custom message for talk
        //  i.e: Join me tomorrow, Join me next week, "Up next in room...", "Join me today..."
        // TODO: Maybe handle timezone?

        var talk = await engagementManager.GetTalkAsync(primaryKey);
        var engagement = await engagementManager.GetAsync(talk.Id);

        var statusText = $"My talk: {talk.Name} ({talk.UrlForTalk})";

        statusText += " at " + engagement.Name;
        statusText += $" is starting at {talk.StartDateTime:f}";
        statusText += $" in room {talk.TalkLocation}";

        statusText += " Come see it!";
        if (engagement.Comments is not null)
        {
            statusText += " Comments" + engagement.Comments;
        }

        if (statusText.Length + 1 >= MaxPostLength)
        {
            var newLength = MaxPostLength - statusText.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }

        logger.LogDebug("Composed Bluesky Posts '{StatusText}'", statusText);
        return statusText;
    }
}