using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IYouTubeSourceManager youTubeSourceManager,
    IEngagementManager engagementManager,
    IMessageTemplateDataStore messageTemplateDataStore,
    ILogger<ProcessScheduledItemFired> logger)
{
    const int MaxTweetLength = 240;

    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_scheduled_item_fired`
    [Function(ConfigurationFunctionNames.TwitterProcessScheduledItemFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)]
    public async Task<TwitterTweetMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.TwitterProcessScheduledItemFired, startedAt);
        
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

            // Attempt Scriban template rendering; fall back to per-type message construction if unavailable
            string? tweetText = null;
            var messageType = scheduledItem.ItemType switch
            {
                ScheduledItemType.Engagements => MessageTemplates.MessageTypes.NewSpeakingEngagement,
                ScheduledItemType.Talks => MessageTemplates.MessageTypes.ScheduledItem,
                ScheduledItemType.SyndicationFeedSources => MessageTemplates.MessageTypes.NewSyndicationFeedItem,
                ScheduledItemType.YouTubeSources => MessageTemplates.MessageTypes.NewYouTubeItem,
                _ => MessageTemplates.MessageTypes.RandomPost
            };            var messageTemplate = await messageTemplateDataStore.GetAsync(MessageTemplates.Platforms.Twitter, messageType);
            if (!string.IsNullOrWhiteSpace(messageTemplate?.Template))
                tweetText = await TryRenderTemplateAsync(scheduledItem, messageTemplate.Template);

            if (tweetText is null)
            {
                // Fallback: per-type message construction
                switch (scheduledItem.ItemType)
                {
                    case ScheduledItemType.Engagements:
                        tweetText = await GetPostForEngagement(scheduledItem.ItemPrimaryKey);
                        break;
                    case ScheduledItemType.Talks:
                        tweetText = await GetPostForTalk(scheduledItem.ItemPrimaryKey);
                        break;
                    case ScheduledItemType.SyndicationFeedSources:
                        tweetText = await GetPostForSyndicationSource(scheduledItem.ItemPrimaryKey);
                        break;
                    case ScheduledItemType.YouTubeSources:
                        tweetText = await GetPostForYouTubeSource(scheduledItem.ItemPrimaryKey);
                        break;
                    default:
                        logger.LogError("The table name '{TableName}' is not supported", scheduledItem.ItemTableName);
                        return null;
                }
            }
            var properties = new Dictionary<string, string>
            {
                { "tableName", scheduledItem.ItemTableName },
                { "primaryKey", scheduledItem.ItemPrimaryKey.ToString() },
                { "text", tweetText }
            };
            logger.LogCustomEvent(Metrics.TwitterProcessScheduledItemFired, properties);
            logger.LogDebug("Generated the tweet for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);
            return new TwitterTweetMessage { Text = tweetText, ImageUrl = scheduledItem.ImageUrl };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new scheduled item. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
        finally
        {
            var endedAt = DateTime.UtcNow;
            logger.LogDebug("{FunctionName} ended at: {EndedAt:f}",
                ConfigurationFunctionNames.TwitterProcessScheduledItemFired, endedAt);
        }
    }
    
    private async Task<string> GetPostForSyndicationSource(int primaryKey)
    {
        var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(primaryKey);
        var statusText = "Blog Post: ";
        var url = syndicationFeedSource.ShortenedUrl ?? syndicationFeedSource.Url;
        var postTitle = syndicationFeedSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(syndicationFeedSource.Tags);
        
        if (statusText.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= MaxTweetLength)
        {
            var newLength = MaxTweetLength - statusText.Length - url.Length - hashTagList.Length - 1;
            postTitle = string.Concat(postTitle.AsSpan(0, newLength - 4), "...");
        }
        
        var tweet = $"{statusText} {postTitle} {url} {hashTagList}";
        
        logger.LogDebug("Composed tweet '{Tweet}'", tweet);
        return tweet;
    }

    private async Task<string> GetPostForYouTubeSource(int primaryKey)
    {
        var youTubeSource = await youTubeSourceManager.GetAsync(primaryKey);

        var statusText = "Video: ";
        var url = youTubeSource.ShortenedUrl ?? youTubeSource.Url;
        var postTitle = youTubeSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(youTubeSource.Tags);
        
        if (statusText.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= MaxTweetLength)
        {
            var newLength = MaxTweetLength - statusText.Length - url.Length - hashTagList.Length - 1;
            postTitle = string.Concat(postTitle.AsSpan(0, newLength - 4), "...");
        }
        
        var tweet = $"{statusText} {postTitle} {url} {hashTagList}";
        
        logger.LogDebug("Composed tweet '{Tweet}'", tweet);
        return tweet;
    }

    private async Task<string> GetPostForEngagement(int primaryKey)
    {
        var engagement = await engagementManager.GetAsync(primaryKey);
        
        var statusText = $"I'm speaking at {engagement.Name} ({engagement.Url}) starting on {engagement.StartDateTime:f}";
        var commentsLength = engagement.Comments?.Length ?? 0;
        var comments = engagement.Comments;
        statusText += " " + comments;
        
        if (statusText.Length + comments?.Length + 1 >= MaxTweetLength)
        {
            var newLength = MaxTweetLength - statusText.Length - commentsLength - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
        
        logger.LogDebug("Composed tweet '{StatusText}'", statusText);
        return statusText;
    }
    
    private async Task<string> GetPostForTalk(int primaryKey)
    {

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
        
        if (statusText.Length + 1 >= MaxTweetLength)
        {
            var newLength = MaxTweetLength - statusText.Length - 1;
            statusText = statusText.Substring(0, newLength - 4) + "...";
        }
            
        logger.LogDebug("Composed tweet '{StatusText}'", statusText);
        return statusText;
    }

    private async Task<string?> TryRenderTemplateAsync(ScheduledItem scheduledItem, string templateContent)
    {
        try
        {
            string title = "", url = "", description = "", tags = "";
            switch (scheduledItem.ItemType)
            {
                case ScheduledItemType.SyndicationFeedSources:
                    var feed = await syndicationFeedSourceManager.GetAsync(scheduledItem.ItemPrimaryKey);
                    title = feed.Title;
                    url = feed.ShortenedUrl ?? feed.Url;
                    tags = feed.Tags ?? "";
                    break;
                case ScheduledItemType.YouTubeSources:
                    var yt = await youTubeSourceManager.GetAsync(scheduledItem.ItemPrimaryKey);
                    title = yt.Title;
                    url = yt.ShortenedUrl ?? yt.Url;
                    tags = yt.Tags ?? "";
                    break;
                case ScheduledItemType.Engagements:
                    var engagement = await engagementManager.GetAsync(scheduledItem.ItemPrimaryKey);
                    title = engagement.Name;
                    url = engagement.Url;
                    description = engagement.Comments ?? "";
                    break;
                case ScheduledItemType.Talks:
                    var talk = await engagementManager.GetTalkAsync(scheduledItem.ItemPrimaryKey);
                    title = talk.Name;
                    url = talk.UrlForTalk;
                    description = talk.Comments;
                    break;
                default:
                    return null;
            }

            var template = Template.Parse(templateContent);
            var scriptObject = new ScriptObject();
            scriptObject.Import(new { title, url, description, tags, image_url = scheduledItem.ImageUrl });
            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            var rendered = await template.RenderAsync(context);
            return string.IsNullOrWhiteSpace(rendered) ? null : rendered.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Scriban template rendering failed for Twitter scheduled item {Id}", scheduledItem.Id);
            return null;
        }
    }
}
