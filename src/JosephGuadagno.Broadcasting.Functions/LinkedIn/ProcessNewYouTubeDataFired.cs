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

public class ProcessNewYouTubeDataFired(
    IYouTubeItemManager youTubeItemManager,
    IUserOAuthTokenManager userOAuthTokenManager,
    ILogger<ProcessNewYouTubeDataFired> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInProcessNewYouTubeDataFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInProcessNewYouTubeDataFired, startedAt);

        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        var eventGridData = eventGridEvent.Data.ToString();
        var newYouTubeItemEvent = JsonSerializer.Deserialize<NewYouTubeItemEvent>(eventGridData);
        if (newYouTubeItemEvent == null)
        {
            logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
            return null;
        }
        var youTubeItem = await youTubeItemManager.GetAsync(newYouTubeItemEvent.Id);

        logger.LogDebug("Processing New YouTube Feed Data Fired for '{Id}' with title of '{Title}'", youTubeItem.Id, youTubeItem.Title);

        // Resolve per-user OAuth token — no silent fallback to shared token
        var token = await userOAuthTokenManager.GetByUserAndPlatformAsync(
            youTubeItem.CreatedByEntraOid,
            SocialMediaPlatformIds.LinkedIn);

        if (token is null)
        {
            logger.LogWarning(
                "No OAuth token found for owner {OwnerOid} on LinkedIn — skipping YouTube item {ItemId}",
                LogSanitizer.Sanitize(youTubeItem.CreatedByEntraOid),
                youTubeItem.Id);
            return null;
        }

        var status = ComposeStatus(youTubeItem, token.AccessToken);

        var properties = new Dictionary<string, string>
        {
            {"post", status.Text},
            {"title", youTubeItem.Title},
            {"url", youTubeItem.Url},
            {"id", youTubeItem.Id.ToString()}
        };
        logger.LogCustomEvent(Metrics.LinkedInProcessedNewSyndicationData, properties);
        logger.LogDebug("Done composing LinkedIn status for '{Id}' with title of '{Title}'", youTubeItem.Id, youTubeItem.Title);
        return status;
    }

    private LinkedInPostLink ComposeStatus(YouTubeItem youTubeItem, string accessToken)
    {
        logger.LogDebug("Composing LinkedIn post for Id: '{Id}', Title:'{Title}'", youTubeItem.Id, youTubeItem.Title);
        var statusText = youTubeItem.LastUpdatedOn > youTubeItem.PublicationDate
                ? "Updated Blog Post: "
                : "New Blog Post: ";

        var post = new LinkedInPostLink
        {
            Text = $"{statusText} {youTubeItem.Title} {HashTagLists.BuildHashTagList(youTubeItem.Tags)}",
            Title = youTubeItem.Title,
            LinkUrl = youTubeItem.Url,
            AccessToken = accessToken
        };

        logger.LogDebug("Composed LinkedIn status for '{Id}' with title of '{Title}'", youTubeItem.Id, youTubeItem.Title);

        return post;
    }
}