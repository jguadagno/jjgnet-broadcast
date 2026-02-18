using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    IEngagementManager engagementManager,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IYouTubeSourceManager youTubeSourceManager,
    TelemetryClient telemetryClient,
    ILogger<ProcessScheduledItemFired> logger)
{
    const int MaxFacebookStatusText = 2000;
    
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_scheduled_item_fired`
    [Function(ConfigurationFunctionNames.FacebookProcessScheduledItemFired)]
    [QueueOutput(Queues.FacebookPostStatusToPage)] 
    public async Task<FacebookPostStatus?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.Now;
        logger.LogDebug("Started {FunctionName} at {StartedOn:f}",
            ConfigurationFunctionNames.FacebookProcessScheduledItemFired, startedOn);
        
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
            FacebookPostStatus facebookPostStatus;
            // The scheduled post should always have a message.  We just need to craft the Title and Urls

            switch (scheduledItem.ItemTableName)
            {
                case SourceSystems.Engagements:
                    facebookPostStatus = await GetFacebookPostStatusForEngagement(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.Talks:
                    facebookPostStatus = await GetFacebookPostStatusForTalk(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.SyndicationFeedSources:
                    facebookPostStatus = await GetFacebookPostStatusForSyndicationSource(scheduledItem.ItemPrimaryKey);
                    break;
                case SourceSystems.YouTubeSources:
                    facebookPostStatus = await GetFacebookPostStatusForYouTubeSource(scheduledItem.ItemPrimaryKey);
                    break;
                default:
                    logger.LogError("The table name '{TableName}' is not supported", scheduledItem.ItemTableName);
                    return null;
            }

            telemetryClient.TrackEvent(Metrics.FacebookProcessedScheduledItemFired,
                new Dictionary<string, string>
                {
                    { "tableName", scheduledItem.ItemTableName },
                    { "id", scheduledItem.ItemPrimaryKey.ToString() },
                    { "text", facebookPostStatus.StatusText },
                    { "url", facebookPostStatus.LinkUri }
                });
            logger.LogDebug("Generated the Facebook status for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);
            return facebookPostStatus;
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
                ConfigurationFunctionNames.FacebookProcessScheduledItemFired, endedOn, endedOn - startedOn);
        }
    }
    
    private async Task<FacebookPostStatus> GetFacebookPostStatusForSyndicationSource(int primaryKey)
    {

        var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(primaryKey);

        var statusText = "ICYMI: Blog Post: ";

        var postTitle = syndicationFeedSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(syndicationFeedSource.Tags);
        
        if (statusText.Length + postTitle.Length + 3 + hashTagList.Length >= MaxFacebookStatusText)
        {
            var newLength = MaxFacebookStatusText - statusText.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  $"{statusText} {postTitle} {hashTagList}",
            LinkUri = syndicationFeedSource.Url
        };

        logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }

    private async Task<FacebookPostStatus> GetFacebookPostStatusForYouTubeSource(int primaryKey)
    {

        var youTubeSource = await youTubeSourceManager.GetAsync(primaryKey);

        var statusText = "ICYMI: Video: ";

        var postTitle = youTubeSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(youTubeSource.Tags);

        if (statusText.Length + postTitle.Length + 3 + hashTagList.Length >= MaxFacebookStatusText)
        {
            var newLength = MaxFacebookStatusText - statusText.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }

        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  $"{statusText} {postTitle} {hashTagList}",
            LinkUri = youTubeSource.Url
        };

        logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
    private async Task<FacebookPostStatus> GetFacebookPostStatusForEngagement(int primaryKey)
    {
        // TODO: Account for custom images for engagement
        // TODO: Account for custom message for engagement
        //  i.e: Join me tomorrow, Join me next week
        // TODO: Maybe handle timezone?

        var engagement = await engagementManager.GetAsync(primaryKey);
        
        var statusText = $"I'm speaking at {engagement.Name} ({engagement.Url}) starting on {engagement.StartDateTime:f}\n";
        var commentsLength = engagement.Comments?.Length ?? 0;
        var comments = engagement.Comments;
        statusText += comments;
        
        if (statusText.Length + commentsLength + 1 >= MaxFacebookStatusText)
        {
            var newLength = MaxFacebookStatusText - statusText.Length - commentsLength - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  statusText,
            LinkUri = engagement.Url                
        };

        logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
    
    private async Task<FacebookPostStatus> GetFacebookPostStatusForTalk(int primaryKey)
    {

        // TODO: Account for custom images for talk
        // TODO: Account for custom message for talk
        //  i.e: Join me tomorrow, Join me next week, "Up next in room...", "Join me today..."
        // TODO: Maybe handle timezone?

        var talk = await engagementManager.GetTalkAsync(primaryKey);
        
        var statusText = $"Talk: {talk.Name} ({talk.UrlForTalk}) starting on {talk.StartDateTime:f} to {talk.EndDateTime:t}";
        statusText += $" in room {talk.TalkLocation}";

        var commentsLength = talk.Comments.Length;
        var comments = " Comments: {engagement.Comments}";
        statusText += comments;
        
        if (statusText.Length + commentsLength + 1 >= MaxFacebookStatusText)
        {
            var newLength = MaxFacebookStatusText - statusText.Length - commentsLength - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  statusText,
            LinkUri = talk.UrlForConferenceTalk                
        };

        logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
}