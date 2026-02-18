using System.Text.Json;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessNewYouTubeDataFired(
    IYouTubeSourceManager youtubeSourceManager,
    ILinkedInApplicationSettings linkedInApplicationSettings,
    TelemetryClient telemetryClient,
    ILogger<ProcessNewYouTubeDataFired> logger)
{
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_new_source_data`
    [Function(ConfigurationFunctionNames.LinkedInProcessNewYouTubeDataFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInProcessNewYouTubeDataFired, startedAt);
        
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
        telemetryClient.TrackEvent(Metrics.LinkedInProcessedNewYouTubeData, new Dictionary<string, string>
        {
            {"post", status.Text},
            {"title", youTubeSource.Title},
            {"url", youTubeSource.Url},
            {"id", youTubeSource.Id.ToString()}
        });
        logger.LogDebug("Done composing LinkedIn status for '{Id}' with title of '{Title}'", youTubeSource.Id, youTubeSource.Title);
        return status;
    }
    
    private LinkedInPostLink ComposeStatus(YouTubeSource youTubeSource)
    {
        logger.LogDebug("Composing LinkedIn post for Id: '{Id}', Title:'{Title}'", youTubeSource.Id, youTubeSource.Title);
        var statusText = youTubeSource.LastUpdatedOn > youTubeSource.PublicationDate
                ? "Updated Blog Post: "
                : "New Blog Post: ";
        
        var post = new LinkedInPostLink
        {
            Text = $"{statusText} {youTubeSource.Title} {HashTagLists.BuildHashTagList(youTubeSource.Tags)}",
            Title = youTubeSource.Title,
            LinkUrl = youTubeSource.Url,
            AuthorId = linkedInApplicationSettings.AuthorId,
            AccessToken = linkedInApplicationSettings.AccessToken
        };
        
        logger.LogDebug("Composed LinkedIn status for '{Id}' with title of '{Title}'", youTubeSource.Id, youTubeSource.Title);
        
        return post;
    }
}