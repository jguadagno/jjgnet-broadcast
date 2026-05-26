using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Functions.Services;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class ScheduledItems(
    IScheduledItemManager scheduledItemManager,
    IScheduledItemEventPublisher scheduledItemEventPublisher,
    IFeedCheckManager feedCheckManager,
    ILogger<ScheduledItems> logger)
{
    [Function(ConfigurationFunctionNames.PublishersScheduledItems)]
    public async Task RunAsync([TimerTrigger("%publishers_scheduled_items_cron_settings%")]
        TimerInfo myTimer)
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
            var publishedCount = 0;

            var feedCheck = await feedCheckManager.GetByNameAsync(
                ConfigurationFunctionNames.PublishersScheduledItems, ownerOid
            ) ?? new FeedCheck
            {
                Name = ConfigurationFunctionNames.PublishersScheduledItems,
                LastCheckedFeed = startedAt,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                EntraOId = ownerOid
            };

            foreach (var scheduledItem in userItems)
            {
                try
                {
                    await scheduledItemEventPublisher.PublishAsync(scheduledItem);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to dispatch scheduled item {ScheduledItemId} for owner '{OwnerOid}'",
                        scheduledItem.Id,
                        LogSanitizer.Sanitize(ownerOid));
                    continue;
                }

                var wasSent = await scheduledItemManager.SentScheduledItemAsync(scheduledItem.Id);
                if (wasSent)
                {
                    publishedCount++;
                    logger.LogCustomEvent(Metrics.ScheduledItemFired, scheduledItem.ToDictionary());
                }
                else
                {
                    logger.LogWarning(
                        "Failed to update the sent flag for scheduled item '{ScheduledItemId}'",
                        scheduledItem.Id);
                }
            }

            feedCheck.LastCheckedFeed = startedAt;
            feedCheck.LastUpdatedOn = DateTimeOffset.UtcNow;
            await feedCheckManager.SaveAsync(feedCheck);

            logger.LogInformation("Published {PublishedCount} of {ScheduledItemCount} scheduled item(s) for owner '{OwnerOid}'",
                publishedCount, userItems.Count, LogSanitizer.Sanitize(ownerOid));
        }

        logger.LogDebug("Done publishing the events for scheduled items");
    }
}
