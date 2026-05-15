using System.Text.Json;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessNewSyndicationDataFired(
    ISyndicationFeedItemManager SyndicationFeedItemManager,
    ILogger<ProcessNewSyndicationDataFired> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyProcessNewSyndicationDataFired)]
    [QueueOutput(Queues.BlueskyPostToSend)]
    public async Task<BlueskyPostMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        try
        {
            var startedAt = DateTimeOffset.UtcNow;
            logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
                ConfigurationFunctionNames.BlueskyProcessNewSyndicationDataFired, startedAt);

            // Check to make sure the eventGridEvent.Data is not null
            if (eventGridEvent.Data is null)
            {
                logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
                return null;
            }

            // Process the EventGrid Event
            var eventGridData = eventGridEvent.Data.ToString();
            var syndicationFeedItemEvent = JsonSerializer.Deserialize<NewSyndicationFeedItemEvent>(eventGridData);
            if (syndicationFeedItemEvent is null)
            {
                logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
                return null;
            }
            var SyndicationFeedItem = await SyndicationFeedItemManager.GetAsync(syndicationFeedItemEvent.Id);

            // Create the scheduled BlueSky Posts for it
            logger.LogDebug("Processing New Syndication Feed Data Fired for '{Id}' with title of '{Title}'", SyndicationFeedItem.Id, SyndicationFeedItem.Title);

            // Handle the event - eventGridData to build the post
            // Need to create a PostBuilder to send this based on these docs
            // https://github.com/blowdart/idunno.Bluesky/blob/main/docs/posting.md#links-to-external-web-sites
            var postText = SyndicationFeedItem.ItemLastUpdatedOn > SyndicationFeedItem.PublicationDate
                ? "Updated Blog Post: "
                : "New Blog Post: ";
            postText +=
                $"({SyndicationFeedItem.PublicationDate.Date.ToShortDateString()}): \"{SyndicationFeedItem.Title}.\" RPs and feedback are always appreciated! ";

            var blueskyPostMessage = new BlueskyPostMessage
            {
                Text = postText,
                Url = SyndicationFeedItem.Url,
                ShortenedUrl = SyndicationFeedItem.ShortenedUrl
            };
            if (SyndicationFeedItem.Tags.Count > 0)
            {
                blueskyPostMessage.Hashtags = SyndicationFeedItem.Tags.ToList();
            }

            blueskyPostMessage.CreatedByEntraOid = SyndicationFeedItem.CreatedByEntraOid;
            // Return
            var properties = new Dictionary<string, string>
            {
                {"post", postText},
                {"title", SyndicationFeedItem.Title},
                {"url", SyndicationFeedItem.Url},
                {"id", SyndicationFeedItem.Id.ToString()}
            };
            logger.LogCustomEvent(Metrics.BlueskyProcessedNewSyndicationData, properties);
            logger.LogDebug("Posted to Bluesky: {Title}", SyndicationFeedItem.Title);
            return blueskyPostMessage;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process the new syndication feed data. Exception: {ExceptionMessage}", exception.Message);
            return null;
        }
    }
}