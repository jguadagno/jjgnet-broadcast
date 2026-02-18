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

public class ProcessNewYouTubeDataFired(
    IYouTubeSourceManager youtubeSourceManager,
    TelemetryClient telemetryClient,
    ILogger<ProcessNewYouTubeDataFired> logger)
{

    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_new_source_data`
    [Function(ConfigurationFunctionNames.TwitterProcessNewYouTubeDataFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)] 
    public async Task<string?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.TwitterProcessNewYouTubeDataFired, startedAt);
        
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
        var newYouTubeItemEvent = JsonSerializer.Deserialize<NewYouTubeItemEvent>(eventGridData);
        if (newYouTubeItemEvent == null)
        {
            logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
            return null;
        }
        var youTubeSource = await youtubeSourceManager.GetAsync(newYouTubeItemEvent.Id);

        logger.LogDebug("Composing tweet for  for '{Id}' with title of '{Title}'", youTubeSource.Id, youTubeSource.Title);
            
        var status = ComposeTweet(youTubeSource);

        // Done
        telemetryClient.TrackEvent(Metrics.FacebookProcessedNewYouTubeData, new Dictionary<string, string>
        {
            {"post", status},
            {"title", youTubeSource.Title},
            {"url", youTubeSource.Url},
            {"id", youTubeSource.Id.ToString()}
        });
        logger.LogDebug("Done composing Facebook status for '{Id}' with title of '{Title}'", youTubeSource.Id, youTubeSource.Title);
        return status;
    }
        
    private string ComposeTweet(YouTubeSource youTubeSource)
    {
        const int maxTweetLength = 240;
            
        // Build Tweet
        var tweetStart = youTubeSource.ItemLastUpdatedOn > youTubeSource.PublicationDate ? "Updated Blog Post: " : "New Blog Post: ";
        var url = youTubeSource.ShortenedUrl ?? youTubeSource.Url;
        var postTitle = youTubeSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(youTubeSource.Tags);
        
        if (tweetStart.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= maxTweetLength)
        {
            var newLength = maxTweetLength - tweetStart.Length - url.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }
            
        var tweet = $"{tweetStart} {postTitle} {url} {hashTagList}";
        logger.LogDebug("Composed tweet '{Tweet}'", tweet);
            
        return tweet;
    }
}