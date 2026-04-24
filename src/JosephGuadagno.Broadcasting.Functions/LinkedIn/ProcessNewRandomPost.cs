using System.Text.Json;

using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessNewRandomPost(
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IUserOAuthTokenManager userOAuthTokenManager,
    ILogger<ProcessNewRandomPost> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInProcessRandomPostFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInProcessRandomPostFired, startedAt);

        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            throw new ArgumentNullException(nameof(eventGridEvent.Data), "EventGrid event data cannot be null");
        }

        try
        {
            var eventGridData = eventGridEvent.Data.ToString();
            var source = JsonSerializer.Deserialize<RandomPostEvent>(eventGridData);
            if (source is null)
            {
                logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
                return null;
            }
            var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(source.Id);

            // Resolve per-user OAuth token — no silent fallback to shared token
            var token = await userOAuthTokenManager.GetByUserAndPlatformAsync(
                syndicationFeedSource.CreatedByEntraOid,
                SocialMediaPlatformIds.LinkedIn);

            if (token is null)
            {
                logger.LogWarning(
                    "No OAuth token found for owner {OwnerOid} on LinkedIn — skipping random post {ItemId}",
                    LogSanitizer.Sanitize(syndicationFeedSource.CreatedByEntraOid),
                    syndicationFeedSource.Id);
                return null;
            }

            var statusText = "ICYMI: ";
            var post = new LinkedInPostLink
            {
                Text = $"{statusText} {syndicationFeedSource.Title} {HashTagLists.BuildHashTagList(syndicationFeedSource.Tags)}",
                Title = syndicationFeedSource.Title,
                LinkUrl = syndicationFeedSource.Url,
                AccessToken = token.AccessToken
            };

            var properties = new Dictionary<string, string>
            {
                {"title", syndicationFeedSource.Title},
                {"url", syndicationFeedSource.Url},
                {"post", post.Text}
            };
            logger.LogCustomEvent(Metrics.LinkedInProcessedRandomPost, properties);
            logger.LogDebug("Picked a random post {Title}", syndicationFeedSource.Title);
            return post;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new random post. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}