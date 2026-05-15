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

public class ProcessNewSyndicationDataFired(
    ISyndicationFeedItemManager SyndicationFeedItemManager,
    ILogger<ProcessNewSyndicationDataFired> logger)
{
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_new_source_data`
    [Function(ConfigurationFunctionNames.FacebookProcessNewSyndicationDataFired)]
    [QueueOutput(Queues.FacebookPostStatusToPage)] 
    public async Task<FacebookPostStatus?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookProcessNewSyndicationDataFired, startedAt);
        
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
        var SyndicationFeedItem = await SyndicationFeedItemManager.GetAsync(syndicationFeedItemEvent.Id);

        // Create the Facebook posts for it
        logger.LogDebug("Processing New Syndication Feed Data Fired for '{Id}' with title of '{Title}'", SyndicationFeedItem.Id, SyndicationFeedItem.Title);

        var status = ComposeStatus(SyndicationFeedItem);
        
        // Done
        var properties = new Dictionary<string, string>
        {
            {"post", status.StatusText},
            {"title", SyndicationFeedItem.Title},
            {"url", SyndicationFeedItem.Url},
            {"id", SyndicationFeedItem.Id.ToString()}
        };
        logger.LogCustomEvent(Metrics.FacebookProcessedNewSyndicationData, properties);
        logger.LogDebug("Done composing Facebook status for '{Id}' with title of '{Title}'", SyndicationFeedItem.Id, SyndicationFeedItem.Title);
        return status;
    }
        
    private FacebookPostStatus ComposeStatus(SyndicationFeedItem SyndicationFeedItem)
    {

        const int maxFacebookStatusText = 2000;
        logger.LogDebug("Composing Facebook status for Id: '{Id}', Title:'{Title}'", SyndicationFeedItem.Id, SyndicationFeedItem.Title);

        // Build Facebook Status
        var statusText = SyndicationFeedItem.LastUpdatedOn > SyndicationFeedItem.PublicationDate
            ? "Updated Blog Post: "
            : "New Blog Post: ";

        var postTitle = SyndicationFeedItem.Title;
        var hashTagList = HashTagLists.BuildHashTagList(SyndicationFeedItem.Tags);
        
        if (statusText.Length + postTitle.Length + 3 + hashTagList.Length >= maxFacebookStatusText)
        {
            var newLength = maxFacebookStatusText - statusText.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  $"{statusText} {postTitle} {hashTagList}",
            LinkUri = SyndicationFeedItem.Url,
            CreatedByEntraOid = SyndicationFeedItem.CreatedByEntraOid
        };

        logger.LogDebug(
            "Composed Facebook Status: StatusText={StatusText}, LinkUrl={LinkUri}",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
}