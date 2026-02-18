using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class ScheduledItems(
    IScheduledItemManager scheduledItemManager,
    IEventPublisher eventPublisher,
    IFeedCheckManager feedCheckManager,
    ILogger<ScheduledItems> logger,
    TelemetryClient telemetryClient)
{
    [Function(ConfigurationFunctionNames.PublishersScheduledItems)]
    public async Task RunAsync([TimerTrigger("%publishers_scheduled_items_cron_settings%")] TimerInfo myTimer, ILogger log)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.PublishersScheduledItems, startedAt);

        var configuration = await feedCheckManager.GetByNameAsync(
                                ConfigurationFunctionNames.PublishersScheduledItems
                            ) ??
                            new FeedCheck { LastCheckedFeed = startedAt, LastItemAddedOrUpdated = DateTime.MinValue };

        // Check for items that are due to be fired
        logger.LogDebug("Checking for scheduled items that have not been fired");
        var scheduledItems =
            await scheduledItemManager.GetScheduledItemsToSendAsync();

        // If there are no scheduled items, log it, and exit
        if (scheduledItems.Count == 0)
        {
            configuration.LastCheckedFeed = startedAt;
            await feedCheckManager.SaveAsync(configuration);
            logger.LogDebug("No new scheduled items found");
            return;
        }

        // Publish the events
        var eventsPublished = await eventPublisher.PublishScheduledItemFiredEventsAsync(
            ConfigurationFunctionNames.PublishersScheduledItems, scheduledItems);
        if (!eventsPublished)
        {
            logger.LogError("Failed to publish the events for some scheduled items");
        }
        else
        {
            // Mark the messages as sent
            foreach (var scheduledItem in scheduledItems)
            {
                var wasSent = await scheduledItemManager.SentScheduledItemAsync(scheduledItem.Id);
                if (wasSent)
                {
                    telemetryClient.TrackEvent(Metrics.ScheduledItemFired, scheduledItem.ToDictionary());
                }
                else
                {
                    logger.LogWarning(
                        "Failed to update the sent flag for scheduled items with the id of '{ScheduledItemId}'",
                        scheduledItem.Id);
                }
            }
        }

        // Save the last checked value
        configuration.LastCheckedFeed = startedAt;
        await feedCheckManager.SaveAsync(configuration);
        
        logger.LogDebug("Done publishing the events for schedule items");
    }
}