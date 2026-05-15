using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessNewRandomPost(ISyndicationFeedItemManager SyndicationFeedItemManager, ILogger<ProcessNewRandomPost> logger)
{
    [Function(ConfigurationFunctionNames.TwitterProcessRandomPostFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)]
    public async Task<TwitterTweetMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
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
            var SyndicationFeedItem = await SyndicationFeedItemManager.GetAsync(source.Id);

            // Handle the event - eventGridData to build the tweet
            var hashtags = HashTagLists.BuildHashTagList(SyndicationFeedItem.Tags);
            var status =
                $"ICYMI: ({SyndicationFeedItem.PublicationDate.Date.ToShortDateString()}): \"{SyndicationFeedItem.Title}.\" RTs and feedback are always appreciated! {SyndicationFeedItem.ShortenedUrl} {hashtags}";

            // Return
            var properties = new Dictionary<string, string>
            {
                {"title", SyndicationFeedItem.Title},
                {"url", SyndicationFeedItem.Url},
                {"tweet", status}
            };
            logger.LogCustomEvent(Metrics.TwitterProcessedRandomPost, properties);
            logger.LogDebug("Picked a random post {Title}", SyndicationFeedItem.Title);
            return new TwitterTweetMessage { Text = status };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new random post. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}