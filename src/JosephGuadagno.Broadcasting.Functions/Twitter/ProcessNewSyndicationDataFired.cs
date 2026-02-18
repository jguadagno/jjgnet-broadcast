using System.Text.Json;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;

using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessNewSyndicationDataFired(
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    TelemetryClient telemetryClient,
    ILogger<ProcessNewSyndicationDataFired> logger)
{

    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_new_source_data`
    [Function(ConfigurationFunctionNames.TwitterProcessNewSyndicationDataFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)] 
    public async Task<string?> RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
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
        var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(syndicationFeedItemEvent.Id);

        logger.LogDebug("Composing tweet for  for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);
            
        var status = ComposeTweet(syndicationFeedSource);

        // Done
        telemetryClient.TrackEvent(Metrics.FacebookProcessedNewSyndicationData, new Dictionary<string, string>
        {
            {"post", status},
            {"title", syndicationFeedSource.Title},
            {"url", syndicationFeedSource.Url},
            {"id", syndicationFeedSource.Id.ToString()}
        });
        logger.LogDebug("Done composing Facebook status for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);
        return status;
    }
        
    private string ComposeTweet(SyndicationFeedSource syndicationFeedSource)
    {
        const int maxTweetLength = 240;
            
        // Build Tweet
        var tweetStart = syndicationFeedSource.ItemLastUpdatedOn > syndicationFeedSource.PublicationDate ? "Updated Blog Post: " : "New Blog Post: ";
        var url = syndicationFeedSource.ShortenedUrl ?? syndicationFeedSource.Url;
        var postTitle = syndicationFeedSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(syndicationFeedSource.Tags);
        
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