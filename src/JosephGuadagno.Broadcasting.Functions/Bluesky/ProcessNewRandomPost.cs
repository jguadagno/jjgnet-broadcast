using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Data.Repositories;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Constants = JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessNewRandomPost(SourceDataRepository sourceDataRepository, TelemetryClient telemetryClient, ILogger<ProcessNewRandomPost> logger)
{
    [Function(Constants.ConfigurationFunctionNames.BlueskyProcessRandomPostFired)]
    [QueueOutput(Constants.Queues.BlueskyPostToSend)]
    public async Task<string> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.BlueskyProcessRandomPostFired, startedAt);
        
        // Check to make sure the eventGridEvent.Data is not null
        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        // The Id of the SourceData Record
        var eventGridData = eventGridEvent.Data.ToString();
        var sourceId = System.Text.Json.JsonSerializer.Deserialize<string>(eventGridData);
        var sourceData = await sourceDataRepository.GetAsync(Constants.Tables.SourceData, sourceId);
        if (sourceData is null)
        {
            logger.LogError("The source data for event '{Id}' was not found", eventGridEvent.Data);
            return null;
        }
        
        // Handle the event - eventGridData to build the post
        var hashtags = sourceData.TagsToHashTags();
        var status =
            $"ICYMI: ({sourceData.PublicationDate.ToShortDateString()}): \"{sourceData.Title}.\" RPs and feedback are always appreciated! {sourceData.ShortenedUrl} {hashtags}";
            
        // Return
        telemetryClient.TrackEvent(Constants.Metrics.RandomBlueskySent, new Dictionary<string, string>
        {
            {"title", sourceData.Title}, 
            {"post", status}
        });
        logger.LogDebug("Picked a random post {Title}", sourceData.Title);
        return status;
    }
}