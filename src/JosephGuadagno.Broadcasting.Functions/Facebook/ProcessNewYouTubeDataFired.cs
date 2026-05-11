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

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessNewYouTubeDataFired(
    IYouTubeItemManager YouTubeItemManager,
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
        
        var startedAt = DateTimeOffset.UtcNow;
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
        var YouTubeItem = await YouTubeItemManager.GetAsync(newYouTubeItemEvent.Id);

        // Create the Facebook posts for it
        logger.LogDebug("Processing New YouTube Feed Data Fired for '{Id}' with title of '{Title}'", YouTubeItem.Id, YouTubeItem.Title);
        
        var status = ComposeStatus(YouTubeItem);
        
        // Done
        var properties = new Dictionary<string, string>
        {
            {"post", status.StatusText},
            {"title", YouTubeItem.Title},
            {"url", YouTubeItem.Url},
            {"id", YouTubeItem.Id.ToString()}
        };
        logger.LogCustomEvent(Metrics.FacebookProcessedNewYouTubeData, properties);
        logger.LogDebug("Done composing Facebook status for '{Id}' with title of '{Title}'", YouTubeItem.Id, YouTubeItem.Title);
        return status;
    }
        
    private FacebookPostStatus ComposeStatus(YouTubeItem YouTubeItem)
    {

        const int maxFacebookStatusText = 2000;
        logger.LogDebug("Composing Facebook status for Id: '{Id}', Title:'{Title}'", YouTubeItem.Id, YouTubeItem.Title);
        
        // Build Facebook Status
        var statusText = YouTubeItem.LastUpdatedOn > YouTubeItem.PublicationDate
            ? "Updated Video Post: "
            : "New Video Post: ";

        var postTitle = YouTubeItem.Title;
        var hashTagList = HashTagLists.BuildHashTagList(YouTubeItem.Tags);
        
        if (statusText.Length + postTitle.Length + 3 + hashTagList.Length >= maxFacebookStatusText)
        {
            var newLength = maxFacebookStatusText - statusText.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  $"{statusText} {postTitle} {hashTagList}",
            LinkUri = YouTubeItem.Url,
            CreatedByEntraOid = YouTubeItem.CreatedByEntraOid
        };

        logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
}