using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class RandomPosts(
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IRandomPostSettings randomPostSettings,
    IEventPublisher eventPublisher,
    ILogger<RandomPosts> logger)
{

    [Function(ConfigurationFunctionNames.PublishersRandomPosts)]
    public async Task Run(
        [TimerTrigger("%publishers_random_post_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.PublishersRandomPosts, startedAt);

        // Get the feed items
        // Check for the from date
        var cutoffDate = DateTimeOffset.MinValue;
        if (randomPostSettings.CutoffDate != DateTime.MinValue)
        {
            cutoffDate = randomPostSettings.CutoffDate;
        }

        logger.LogDebug("Getting all items from feed from '{CutoffDate:u}'", cutoffDate);
        var syndicationFeedSource = await syndicationFeedSourceManager.GetRandomSyndicationDataAsync(cutoffDate, randomPostSettings.ExcludedCategories);

        if (syndicationFeedSource is null)
        {
            logger.LogDebug("Could not find a random post from feed since '{CutoffDate:u}'", cutoffDate);
            return;
        }
        
        // Create the event message to post to the topic -- throws EventPublishException on failure
        try
        {
            await eventPublisher.PublishRandomPostsEventsAsync(ConfigurationFunctionNames.PublishersRandomPosts,
                syndicationFeedSource.Id);

            logger.LogCustomEvent(Metrics.RandomPostFired, new Dictionary<string, string>
            {
                {"title", syndicationFeedSource.Title},
                {"url", syndicationFeedSource.Url},
                {"id", syndicationFeedSource.Id.ToString()}
            });

            logger.LogDebug("Latest random post '{RandomSyndicationIdTitleText}' has been published",
                syndicationFeedSource.Title);
        }
        catch (EventPublishException ex)
        {
            logger.LogError(ex, "Failed to publish random post event for '{Title}' (Id: {Id})",
                syndicationFeedSource.Title, syndicationFeedSource.Id);
            logger.LogCustomEvent(Metrics.RandomPostFired, new Dictionary<string, string>
            {
                {"title", syndicationFeedSource.Title},
                {"url", syndicationFeedSource.Url},
                {"id", syndicationFeedSource.Id.ToString()}
            });
            throw;
        }
    }
}
