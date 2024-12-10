using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Data.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Constants = JosephGuadagno.Broadcasting.Domain.Constants;
using Microsoft.ApplicationInsights;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessNewRandomPost(SourceDataRepository sourceDataRepository, TelemetryClient telemetryClient, ILogger<ProcessNewRandomPost> logger)
{
    [Function(Constants.ConfigurationFunctionNames.TwitterProcessRandomPostFired)]
    [QueueOutput(Constants.Queues.TwitterTweetsToSend)]
    public async Task<string> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.TwitterProcessNewSourceData, startedAt);
        
        // Get the Source Data identifier for the event
        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        var eventGridData = eventGridEvent.Data.ToString();
        var sourceId = System.Text.Json.JsonSerializer.Deserialize<string>(eventGridData).Replace("\"", "");
        var sourceData = await sourceDataRepository.GetAsync(Constants.Tables.SourceData, sourceId);
        if (sourceData is null)
        {
            logger.LogError("The source data for event '{Id}' was not found", eventGridEvent.Data);
            return null;
        }
        
        // Handle the event - eventGridData to build the tweet
        var hashtags = sourceData.TagsToHashTags();
        var status =
            $"ICYMI: ({sourceData.PublicationDate.ToShortDateString()}): \"{sourceData.Title}.\" RTs and feedback are always appreciated! {sourceData.ShortenedUrl} {hashtags}";
            
        // Return
        telemetryClient.TrackEvent(Constants.Metrics.RandomTweetSent, new Dictionary<string, string>
        {
            {"title", sourceData.Title}, 
            {"tweet", status}
        });
        logger.LogDebug("Picked a random post {Title}", sourceData.Title);
        return status;
    }
}