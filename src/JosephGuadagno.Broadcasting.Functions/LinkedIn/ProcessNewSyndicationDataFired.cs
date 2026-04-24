using System.Text.Json;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessNewSyndicationDataFired(
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IUserOAuthTokenManager userOAuthTokenManager,
    ILogger<ProcessNewSyndicationDataFired> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInProcessNewSyndicationDataFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInProcessNewSyndicationDataFired, startedAt);

        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        var eventGridData = eventGridEvent.Data.ToString();
        var syndicationFeedItemEvent = JsonSerializer.Deserialize<NewSyndicationFeedItemEvent>(eventGridData);
        if (syndicationFeedItemEvent is null)
        {
            logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
            return null;
        }
        var syndicationFeedSource = await syndicationFeedSourceManager.GetAsync(syndicationFeedItemEvent.Id);

        logger.LogDebug("Processing New Syndication Feed Data Fired for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);

        // Resolve per-user OAuth token — no silent fallback to shared token
        var token = await userOAuthTokenManager.GetByUserAndPlatformAsync(
            syndicationFeedSource.CreatedByEntraOid,
            SocialMediaPlatformIds.LinkedIn);

        if (token is null)
        {
            logger.LogWarning(
                "No OAuth token found for owner {OwnerOid} on LinkedIn — skipping syndication item {ItemId}",
                LogSanitizer.Sanitize(syndicationFeedSource.CreatedByEntraOid),
                syndicationFeedSource.Id);
            return null;
        }

        var status = ComposeStatus(syndicationFeedSource, token.AccessToken);

        var properties = new Dictionary<string, string>
        {
            {"post", status.Text},
            {"title", syndicationFeedSource.Title},
            {"url", syndicationFeedSource.Url},
            {"id", syndicationFeedSource.Id.ToString()}
        };
        logger.LogCustomEvent(Metrics.LinkedInProcessedNewSyndicationData, properties);
        logger.LogDebug("Done composing LinkedIn status for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);
        return status;
    }

    private LinkedInPostLink ComposeStatus(SyndicationFeedSource syndicationFeedSource, string accessToken)
    {
        logger.LogDebug("Composing LinkedIn post for Id: '{Id}', Title:'{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);
        var statusText = syndicationFeedSource.LastUpdatedOn > syndicationFeedSource.PublicationDate
                ? "Updated Blog Post: "
                : "New Blog Post: ";

        var post = new LinkedInPostLink
        {
            Text = $"{statusText} {syndicationFeedSource.Title} {HashTagLists.BuildHashTagList(syndicationFeedSource.Tags)}",
            Title = syndicationFeedSource.Title,
            LinkUrl = syndicationFeedSource.Url,
            AccessToken = accessToken
        };

        logger.LogDebug("Composed LinkedIn status for '{Id}' with title of '{Title}'", syndicationFeedSource.Id, syndicationFeedSource.Title);

        return post;
    }
}