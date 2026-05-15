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
    IUserCollectorScheduledItemManager userCollectorScheduledItemManager,
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

        var activeConfigs = await userCollectorScheduledItemManager.GetAllActiveAsync();
        if (activeConfigs.Count == 0)
        {
            logger.LogDebug("No active scheduled item configurations found");
            return;
        }

        logger.LogDebug("Checking for scheduled items that have not been fired");
        var allScheduledItems = await scheduledItemManager.GetScheduledItemsToSendAsync();

        if (allScheduledItems.Count == 0)
        {
            foreach (var cfg in activeConfigs)
            {
                var noop = await feedCheckManager.GetByNameAsync(
                    ConfigurationFunctionNames.PublishersScheduledItems, cfg.CreatedByEntraOid
                ) ?? new FeedCheck
                {
                    Name = ConfigurationFunctionNames.PublishersScheduledItems,
                    LastCheckedFeed = startedAt,
                    LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                    EntraOId = cfg.CreatedByEntraOid
                };
                noop.LastCheckedFeed = startedAt;
                await feedCheckManager.SaveAsync(noop);
            }
            logger.LogDebug("No new scheduled items found");
            return;
        }

        foreach (var config in activeConfigs)
        {
            var userItems = allScheduledItems
                .Where(item => item.CreatedByEntraOid == config.CreatedByEntraOid)
                .ToList();

            var feedCheck = await feedCheckManager.GetByNameAsync(
                ConfigurationFunctionNames.PublishersScheduledItems, config.CreatedByEntraOid
            ) ?? new FeedCheck
            {
                Name = ConfigurationFunctionNames.PublishersScheduledItems,
                LastCheckedFeed = startedAt,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                EntraOId = config.CreatedByEntraOid
            };

            if (userItems.Count == 0)
            {
                feedCheck.LastCheckedFeed = startedAt;
                await feedCheckManager.SaveAsync(feedCheck);
                logger.LogDebug("No new scheduled items for owner '{OwnerOid}'", config.CreatedByEntraOid);
                continue;
            }

            try
            {
                await eventPublisher.PublishScheduledItemFiredEventsAsync(
                    ConfigurationFunctionNames.PublishersScheduledItems, userItems);
            }
            catch (EventPublishException ex)
            {
                logger.LogError(ex, "Failed to publish scheduled item events for owner '{OwnerOid}' ({Count} item(s))",
                    config.CreatedByEntraOid, userItems.Count);
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
                userItems.Count, config.CreatedByEntraOid);
        }

        logger.LogDebug("Done publishing the events for scheduled items");
    }
}
