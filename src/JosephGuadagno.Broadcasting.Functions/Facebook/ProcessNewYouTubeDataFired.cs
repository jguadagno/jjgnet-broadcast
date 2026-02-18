using System.Text.Json;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessNewYouTubeDataFired(
    IYouTubeSourceManager youtubeSourceManager,
    TelemetryClient telemetryClient,
    ILogger<ProcessNewYouTubeDataFired> logger)
{
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_new_source_data`
    [Function(ConfigurationFunctionNames.FacebookProcessNewYouTubeDataFired)]
    [QueueOutput(Queues.FacebookPostStatusToPage)] 
    public async Task<FacebookPostStatus?> RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookProcessNewYouTubeDataFired, startedAt);
        
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

        // Create the Facebook posts for it
        logger.LogDebug("Processing New YouTube Feed Data Fired for '{Id}' with title of '{Title}'", youTubeSource.Id, youTubeSource.Title);
        
        var status = ComposeStatus(youTubeSource);
        
        // Done
        telemetryClient.TrackEvent(Metrics.FacebookProcessedNewYouTubeData, new Dictionary<string, string>
        {
            {"post", status.StatusText},
            {"title", youTubeSource.Title},
            {"url", youTubeSource.Url},
            {"id", youTubeSource.Id.ToString()}
        });
        logger.LogDebug("Done composing Facebook status for '{Id}' with title of '{Title}'", youTubeSource.Id, youTubeSource.Title);
        return status;
    }
        
    private FacebookPostStatus ComposeStatus(YouTubeSource youTubeSource)
    {

        const int maxFacebookStatusText = 2000;
        logger.LogDebug("Composing Facebook status for Id: '{Id}', Title:'{Title}'", youTubeSource.Id, youTubeSource.Title);
        
        // Build Facebook Status
        var statusText = youTubeSource.LastUpdatedOn > youTubeSource.PublicationDate
            ? "Updated Video Post: "
            : "New Video Post: ";

        var postTitle = youTubeSource.Title;
        var hashTagList = HashTagLists.BuildHashTagList(youTubeSource.Tags);
        
        if (statusText.Length + postTitle.Length + 3 + hashTagList.Length >= maxFacebookStatusText)
        {
            var newLength = maxFacebookStatusText - statusText.Length - hashTagList.Length - 1;
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
}