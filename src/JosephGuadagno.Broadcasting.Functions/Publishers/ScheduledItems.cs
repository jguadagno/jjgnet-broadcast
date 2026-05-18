using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class ScheduledItems(
    IScheduledItemManager scheduledItemManager,
    IEventPublisher eventPublisher,
    IFeedCheckManager feedCheckManager,
    ILogger<ScheduledItems> logger)
{
    [Function(ConfigurationFunctionNames.PublishersScheduledItems)]
    public async Task RunAsync([TimerTrigger("%publishers_scheduled_items_cron_settings%")] TimerInfo myTimer, ILogger log)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.PublishersScheduledItems, startedAt);

        logger.LogDebug("Checking for scheduled items that have not been fired");
        var allScheduledItems = await scheduledItemManager.GetScheduledItemsToSendAsync();

        if (allScheduledItems.Count == 0)
        {
            logger.LogDebug("No new scheduled items found");
            return;
        }

        var itemsByOwner = allScheduledItems
            .Where(item => !string.IsNullOrWhiteSpace(item.CreatedByEntraOid))
            .GroupBy(item => item.CreatedByEntraOid!)
            .ToList();

        foreach (var ownerGroup in itemsByOwner)
        {
            var ownerOid = ownerGroup.Key;
            var userItems = ownerGroup.ToList();

            var feedCheck = await feedCheckManager.GetByNameAsync(
                ConfigurationFunctionNames.PublishersScheduledItems, ownerOid
            ) ?? new FeedCheck
            {
                Name = ConfigurationFunctionNames.PublishersScheduledItems,
                LastCheckedFeed = startedAt,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                EntraOId = ownerOid
            };

            try
            {
                await eventPublisher.PublishScheduledItemFiredEventsAsync(
                    ConfigurationFunctionNames.PublishersScheduledItems, userItems);
            }
            catch (EventPublishException ex)
            {
                logger.LogError(ex, "Failed to publish scheduled item events for owner '{OwnerOid}' ({Count} item(s))",
                    ownerOid, userItems.Count);
                foreach (var scheduledItem in userItems)
                {
                    logger.LogCustomEvent(Metrics.ScheduledItemFired, scheduledItem.ToDictionary());
                }
                throw;
            }

            foreach (var scheduledItem in userItems)
            {
                var wasSent = await scheduledItemManager.SentScheduledItemAsync(scheduledItem.Id);
                if (wasSent)
                {
                    logger.LogCustomEvent(Metrics.ScheduledItemFired, scheduledItem.ToDictionary());
                }
                else
                {
                    logger.LogWarning(
                        "Failed to update the sent flag for scheduled items with the id of '{ScheduledItemId}'",
                        scheduledItem.Id);
                }
            }

            feedCheck.LastCheckedFeed = startedAt;
            feedCheck.LastUpdatedOn = DateTimeOffset.UtcNow;
            await feedCheckManager.SaveAsync(feedCheck);

            logger.LogInformation("Published {Count} scheduled item(s) for owner '{OwnerOid}'",
                userItems.Count, ownerOid);
        }

        logger.LogDebug("Done publishing the events for scheduled items");
    }
}
