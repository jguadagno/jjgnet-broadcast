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

public class ProcessNewSyndicationDataFired(
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    ILinkedInApplicationSettings linkedInApplicationSettings,
    TelemetryClient telemetryClient,
    ILogger<ProcessNewSyndicationDataFired> logger)
{
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_new_source_data`
    [Function(ConfigurationFunctionNames.LinkedInProcessNewSyndicationDataFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInProcessNewSyndicationDataFired, startedAt);
        
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

        // Create the Facebook posts for it
        logger.LogDebug("Processing New Syndication Feed Data Fired for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);

        var status = ComposeStatus(syndicationFeedSource);
        
        // Done
        telemetryClient.TrackEvent(Metrics.LinkedInProcessedNewSyndicationData, new Dictionary<string, string>
        {
            {"post", status.Text},
            {"title", syndicationFeedSource.Title},
            {"url", syndicationFeedSource.Url},
            {"id", syndicationFeedSource.Id.ToString()}
        });
        logger.LogDebug("Done composing LinkedIn status for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);
        return status;
    }
    
    private LinkedInPostLink ComposeStatus(SyndicationFeedSource syndicationFeedSource)
    {
        logger.LogDebug("Composing LinkedIn post for Id: '{Id}', Title:'{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);
        var statusText = syndicationFeedSource.LastUpdatedOn > syndicationFeedSource.PublicationDate
                ? "Updated Blog Post: "
                : "New Blog Post: ";
        
        var post = new LinkedInPostLink
        {
            Text = $"{statusText} {syndicationFeedSource.Title} {HashTagLists.BuildHashTagList(syndicationFeedSource.Tags)}",
            Title = syndicationFeedSource.Title,
            LinkUrl = syndicationFeedSource.Url,
            AuthorId = linkedInApplicationSettings.AuthorId,
            AccessToken = linkedInApplicationSettings.AccessToken
        };

        logger.LogDebug("Composed LinkedIn status for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);
        
        return post;
    }
}