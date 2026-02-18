using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;

using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessNewRandomPost(ISyndicationFeedSourceManager syndicationFeedSourceManager, TelemetryClient telemetryClient, ILogger<ProcessNewRandomPost> logger)
{
    [Function(ConfigurationFunctionNames.TwitterProcessRandomPostFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)]
    public async Task<string?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.TwitterProcessRandomPostFired, startedAt);

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
            var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(source.Id);

            // Handle the event - eventGridData to build the tweet
            var hashtags = HashTagLists.BuildHashTagList(syndicationFeedSource.Tags);
            var status =
                $"ICYMI: ({syndicationFeedSource.PublicationDate.Date.ToShortDateString()}): \"{syndicationFeedSource.Title}.\" RTs and feedback are always appreciated! {syndicationFeedSource.ShortenedUrl} {hashtags}";

            // Return
            telemetryClient.TrackEvent(Metrics.TwitterProcessedRandomPost, new Dictionary<string, string>
            {
                {"title", syndicationFeedSource.Title},
                {"url", syndicationFeedSource.Url},
                {"tweet", status}
            });
            logger.LogDebug("Picked a random post {Title}", syndicationFeedSource.Title);
            return status;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new random post. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}