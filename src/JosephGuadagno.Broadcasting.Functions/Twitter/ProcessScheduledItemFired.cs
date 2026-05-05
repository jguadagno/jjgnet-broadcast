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

public class ProcessScheduledItemFired(
    IScheduledItemManager scheduledItemManager,
    ITwitterManager twitterManager,
    ILogger<ProcessScheduledItemFired> logger)
{
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=twitter_process_scheduled_item_fired`
    [Function(ConfigurationFunctionNames.TwitterProcessScheduledItemFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)]
    public async Task<TwitterTweetMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.TwitterProcessScheduledItemFired, startedAt);

        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        try
        {
            var eventGridData = eventGridEvent.Data.ToString();
            var scheduledItemFiredEvent = JsonSerializer.Deserialize<ScheduledItemFiredEvent>(eventGridData);
            if (scheduledItemFiredEvent is null)
            {
                logger.LogError("Failed to parse the TableEvent data for event '{Id}'", eventGridEvent.Id);
                return null;
            }
            var scheduledItem = await scheduledItemManager.GetAsync(scheduledItemFiredEvent.Id);

            logger.LogDebug("Processing the event '{Id}' for '{TableName}', '{PartitionKey}'",
                eventGridEvent.Id, scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);

            var tweetText = await twitterManager.ComposeMessageAsync(scheduledItem);

            var properties = new Dictionary<string, string>
            {
                { "tableName", scheduledItem.ItemTableName },
                { "primaryKey", scheduledItem.ItemPrimaryKey.ToString() },
                { "text", tweetText }
            };
            logger.LogCustomEvent(Metrics.TwitterProcessScheduledItemFired, properties);
            logger.LogDebug("Generated the tweet for {TableName}, {PrimaryKey}",
                scheduledItem.ItemTableName, scheduledItem.ItemPrimaryKey);
            return new TwitterTweetMessage { Text = tweetText, ImageUrl = scheduledItem.ImageUrl };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new scheduled item. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
        finally
        {
            var endedAt = DateTimeOffset.UtcNow;
            logger.LogDebug("{FunctionName} ended at: {EndedAt:f}",
                ConfigurationFunctionNames.TwitterProcessScheduledItemFired, endedAt);
        }
    }
}

