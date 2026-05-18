using System.Text.Json;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessNewSyndicationDataFired(
    ISyndicationFeedItemManager syndicationFeedItemManager,
    ILogger<ProcessNewSyndicationDataFired> logger)
{

    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_new_source_data`
    [Function(ConfigurationFunctionNames.TwitterProcessNewSyndicationDataFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)] 
    public async Task<TwitterTweetMessage?> RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.TwitterProcessNewSyndicationDataFired, startedAt);
        
        // Get the Source Data identifier for the event
        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        // Get the Source Data identifier for the event
        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        var eventGridData = eventGridEvent.Data.ToString();
        var syndicationFeedItemEvent = JsonSerializer.Deserialize<NewSyndicationFeedItemEvent>(eventGridData);
        if (syndicationFeedItemEvent is null)
        {
            logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
            return null;
        }
        var syndicationFeedItem = await syndicationFeedItemManager.GetAsync(syndicationFeedItemEvent.Id);

        logger.LogDebug("Composing tweet for  for '{Id}' with title of '{Title}'", syndicationFeedItem.Id, syndicationFeedItem.Title);
            
        var status = ComposeTweet(syndicationFeedItem);

        // Done
        var properties = new Dictionary<string, string>
        {
            {"post", status},
            {"title", syndicationFeedItem.Title},
            {"url", syndicationFeedItem.Url},
            {"id", syndicationFeedItem.Id.ToString()}
        };
        logger.LogCustomEvent(Metrics.FacebookProcessedNewSyndicationData, properties);
        logger.LogDebug("Done composing Facebook status for '{Id}' with title of '{Title}'", syndicationFeedItem.Id, syndicationFeedItem.Title);
        return new TwitterTweetMessage { Text = status, CreatedByEntraOid = syndicationFeedItem.CreatedByEntraOid };
    }
        
    private string ComposeTweet(SyndicationFeedItem syndicationFeedItem)
    {
        const int maxTweetLength = 240;
            
        // Build Tweet
        var tweetStart = syndicationFeedItem.ItemLastUpdatedOn > syndicationFeedItem.PublicationDate ? "Updated Blog Post: " : "New Blog Post: ";
        var url = syndicationFeedItem.ShortenedUrl ?? syndicationFeedItem.Url;
        var postTitle = syndicationFeedItem.Title;
        var hashTagList = HashTagLists.BuildHashTagList(syndicationFeedItem.Tags);
        
        if (tweetStart.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= maxTweetLength)
        {
            var newLength = maxTweetLength - tweetStart.Length - url.Length - hashTagList.Length - 1;
            postTitle = postTitle[..(newLength - 4)] + "...";
        }
            
        var tweet = $"{tweetStart} {postTitle} {url} {hashTagList}";
        logger.LogDebug("Composed tweet '{Tweet}'", tweet);
            
        return tweet;
    }
}