using System.Text.Json;

using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessNewRandomPost(ISyndicationFeedSourceManager syndicationFeedSourceManager, ILogger<ProcessNewRandomPost> logger)
{
    [Function(ConfigurationFunctionNames.FacebookProcessRandomPostFired)]
    [QueueOutput(Queues.FacebookPostStatusToPage)]
    public async Task<FacebookPostStatus?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookProcessRandomPostFired, startedAt);

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

            // Handle the event - compose the Facebook post status
            const int maxFacebookStatusText = 2000;
            var statusText = "ICYMI: Blog Post: ";
            var postTitle = syndicationFeedSource.Title;
            var hashTagList = HashTagLists.BuildHashTagList(syndicationFeedSource.Tags);

            if (statusText.Length + postTitle.Length + 3 + hashTagList.Length >= maxFacebookStatusText)
            {
                var newLength = maxFacebookStatusText - statusText.Length - hashTagList.Length - 1;
                postTitle = postTitle.Substring(0, newLength - 4) + "...";
            }

            var facebookPostStatus = new FacebookPostStatus
            {
                StatusText = $"{statusText} {postTitle} {hashTagList}",
                LinkUri = syndicationFeedSource.Url
            };

            // Return
            var properties = new Dictionary<string, string>
            {
                {"title", syndicationFeedSource.Title},
                {"url", syndicationFeedSource.Url},
                {"post", facebookPostStatus.StatusText}
            };
            logger.LogCustomEvent(Metrics.FacebookProcessedRandomPost, properties);
            logger.LogDebug("Picked a random post {Title}", syndicationFeedSource.Title);
            return facebookPostStatus;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new random post. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}
