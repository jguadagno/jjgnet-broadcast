using System.Text.Json;

using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessNewRandomPost(ISyndicationFeedSourceManager syndicationFeedSourceManager, TelemetryClient telemetryClient, ILogger<ProcessNewRandomPost> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyProcessRandomPostFired)]
    [QueueOutput(Queues.BlueskyPostToSend)]
    public async Task<BlueskyPostMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.BlueskyProcessRandomPostFired, startedAt);
        
        // Check to make sure the eventGridEvent.Data is not null
        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            throw new ArgumentNullException(nameof(eventGridEvent.Data), "EventGrid event data cannot be null");
        }

        try
        {
            var eventGridData = eventGridEvent.Data.ToString();
            var source = JsonSerializer.Deserialize<RandomPostEvent>(eventGridData);
            if (source is null)
            {
                logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
                return null;
            }
            var sourceData = await syndicationFeedSourceManager.GetAsync(source.Id);

            // Handle the event - eventGridData to build the post
            // Need to create a PostBuilder to send this based on these docs
            // https://github.com/blowdart/idunno.Bluesky/blob/main/docs/posting.md#links-to-external-web-sites
            var postText =
                $"ICYMI: ({sourceData.PublicationDate.Date.ToShortDateString()}): \"{sourceData.Title}.\" RPs and feedback are always appreciated! ";

            var blueskyPostMessage = new BlueskyPostMessage
            {
                Text = postText,
                Url = sourceData.Url,
                ShortenedUrl = sourceData.ShortenedUrl
            };
            if (!string.IsNullOrEmpty(sourceData.Tags))
            {
                blueskyPostMessage.Hashtags = sourceData.Tags.Split(',').ToList();
            }

            // Return
            telemetryClient.TrackEvent(Metrics.BlueskyProcessedRandomPost, new Dictionary<string, string>
            {
                {"title", sourceData.Title},
                {"url", sourceData.Url},
                {"post", postText}
            });
            logger.LogDebug("Picked a random post {Title}", sourceData.Title);
            return blueskyPostMessage;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new random post. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}