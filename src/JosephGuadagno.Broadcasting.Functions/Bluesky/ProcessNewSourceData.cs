using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Constants = JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessNewSourceData(SourceDataRepository sourceDataRepository, TelemetryClient telemetryClient, ILogger<ProcessNewRandomPost> logger)
{
    [Function(Constants.ConfigurationFunctionNames.BlueskyProcessNewSourceDataFired)]
    [QueueOutput(Constants.Queues.BlueskyPostToSend)]
    public async Task<BlueskyPostMessage> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.BlueskyProcessNewSourceDataFired, startedAt);
        
        // Check to make sure the eventGridEvent.Data is not null
        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        // The Id of the SourceData Record
        var eventGridData = eventGridEvent.Data.ToString();
        var sourceId = System.Text.Json.JsonSerializer.Deserialize<string>(eventGridData).Replace("\"", "");
        var sourceData = await sourceDataRepository.GetAsync(SourceSystems.SyndicationFeed, sourceId);
        if (sourceData is null)
        {
            logger.LogError("The source data for event '{Id}' was not found", eventGridEvent.Data);
            return null;
        }
        
        // Handle the event - eventGridData to build the post
        // Need to create a PostBuilder to send this based on these docs
        // https://github.com/blowdart/idunno.Bluesky/blob/main/docs/posting.md#links-to-external-web-sites
        var postText = sourceData.SourceSystem switch
        {
            nameof(SourceSystems.SyndicationFeed) => sourceData.UpdatedOnDate > sourceData.PublicationDate
                ? "Updated Blog Post: "
                : "New Blog Post: ",
            nameof(SourceSystems.YouTube) => sourceData.UpdatedOnDate > sourceData.PublicationDate
                ? "Updated Video: "
                : "New Video: ",
            _ => string.Empty
        };
        postText +=
            $"({sourceData.PublicationDate.ToShortDateString()}): \"{sourceData.Title}.\" RPs and feedback are always appreciated! ";

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
        telemetryClient.TrackEvent(Constants.Metrics.ProcessedNewBlueskyPost, new Dictionary<string, string>
        {
            {"title", sourceData.Title}, 
            {"url", sourceData.Url},
            {"post", postText}
        });
        logger.LogDebug("Posted to Bluesky: {Title}", sourceData.Title);
        return blueskyPostMessage;
    }
}