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

public class ProcessNewYouTubeDataFired(
    IYouTubeSourceManager youtubeSourceManager,
    TelemetryClient telemetryClient,
    ILogger<ProcessNewYouTubeDataFired> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyProcessNewYouTubeDataFired)]
    [QueueOutput(Queues.BlueskyPostToSend)]
    public async Task<BlueskyPostMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        try
        {
            var startedAt = DateTime.UtcNow;
            logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
                ConfigurationFunctionNames.BlueskyProcessNewYouTubeDataFired, startedAt);

            // Check to make sure the eventGridEvent.Data is not null
            if (eventGridEvent.Data is null)
            {
                logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
                return null;
            }

            // Process the EventGrid Event
            var eventGridData = eventGridEvent.Data.ToString();
            var newYouTubeItemEvent = JsonSerializer.Deserialize<NewYouTubeItemEvent>(eventGridData);
            if (newYouTubeItemEvent == null)
            {
                logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
                return null;
            }
            var youTubeSource = await youtubeSourceManager.GetAsync(newYouTubeItemEvent.Id);

            // Create the scheduled BlueSky for it
            logger.LogDebug("Processing New YouTube Feed Data Fired for '{Id}' with title of '{Title}'", youTubeSource.Id, youTubeSource.Title);

            // Handle the event - eventGridData to build the post
            // Need to create a PostBuilder to send this based on these docs
            // https://github.com/blowdart/idunno.Bluesky/blob/main/docs/posting.md#links-to-external-web-sites
            var postText = youTubeSource.ItemLastUpdatedOn > youTubeSource.PublicationDate
                ? "Updated Video Post: "
                : "New Video Post: ";
            postText +=
                $"({youTubeSource.PublicationDate.Date.ToShortDateString()}): \"{youTubeSource.Title}.\" RPs and feedback are always appreciated! ";

            var blueskyPostMessage = new BlueskyPostMessage
            {
                Text = postText,
                Url = youTubeSource.Url,
                ShortenedUrl = youTubeSource.ShortenedUrl
            };
            if (!string.IsNullOrEmpty(youTubeSource.Tags))
            {
                blueskyPostMessage.Hashtags = youTubeSource.Tags.Split(',').ToList();
            }

            // Return
            telemetryClient.TrackEvent(Metrics.BlueskyProcessedNewYouTubeData, new Dictionary<string, string>
            {
                {"post", postText},
                {"title", youTubeSource.Title},
                {"url", youTubeSource.Url},
                {"id", youTubeSource.Id.ToString()}
            });
            logger.LogDebug("Posted to Bluesky: {Title}", youTubeSource.Title);
            return blueskyPostMessage;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process the new YouTube data. Exception: {ExceptionMessage}", exception.Message);
            return null;
        }
    }
}