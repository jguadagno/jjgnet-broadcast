using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class RandomPosts(
    ISyndicationFeedItemManager SyndicationFeedItemManager,
    IRandomPostSettings randomPostSettings,
    IEventPublisher eventPublisher,
    ILogger<RandomPosts> logger)
{

    [Function(ConfigurationFunctionNames.PublishersRandomPosts)]
    public async Task Run(
        [TimerTrigger("%publishers_random_post_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.PublishersRandomPosts, startedAt);

        // Get the feed items
        // Check for the from date
        var cutoffDate = DateTimeOffset.MinValue;
        if (randomPostSettings.CutoffDate != DateTimeOffset.MinValue)
        {
            cutoffDate = randomPostSettings.CutoffDate;
        }

        logger.LogDebug("Getting all items from feed from '{CutoffDate:u}'", cutoffDate);
        var ownerOid = await SyndicationFeedItemManager.GetCollectorOwnerOidAsync();
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            logger.LogWarning("Could not resolve a collector owner OID from existing syndication feed source records.");
            return;
        }
        var SyndicationFeedItem = await SyndicationFeedItemManager.GetRandomSyndicationDataAsync(ownerOid, cutoffDate, randomPostSettings.ExcludedCategories);

        if (SyndicationFeedItem is null)
        {
            logger.LogDebug("Could not find a random post from feed since '{CutoffDate:u}'", cutoffDate);
            return;
        }
        
        // Create the event message to post to the topic -- throws EventPublishException on failure
        try
        {
            await eventPublisher.PublishRandomPostsEventsAsync(ConfigurationFunctionNames.PublishersRandomPosts,
                SyndicationFeedItem.Id);

            logger.LogCustomEvent(Metrics.RandomPostFired, new Dictionary<string, string>
            {
                {"title", SyndicationFeedItem.Title},
                {"url", SyndicationFeedItem.Url},
                {"id", SyndicationFeedItem.Id.ToString()}
            });

            logger.LogDebug("Latest random post '{RandomSyndicationIdTitleText}' has been published",
                SyndicationFeedItem.Title);
        }
        catch (EventPublishException ex)
        {
            logger.LogError(ex, "Failed to publish random post event for '{Title}' (Id: {Id})",
                SyndicationFeedItem.Title, SyndicationFeedItem.Id);
            logger.LogCustomEvent(Metrics.RandomPostFired, new Dictionary<string, string>
            {
                {"title", SyndicationFeedItem.Title},
                {"url", SyndicationFeedItem.Url},
                {"id", SyndicationFeedItem.Id.ToString()}
            });
            throw;
        }
    }
}
