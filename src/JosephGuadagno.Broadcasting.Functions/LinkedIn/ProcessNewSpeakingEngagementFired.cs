using System.Text.Json;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessNewSpeakingEngagementFired(
    IEngagementManager engagementManager,
    ILinkedInManager linkedInManager,
    IUserOAuthTokenManager userOAuthTokenManager,
    ILogger<ProcessNewSpeakingEngagementFired> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInProcessNewSpeakingEngagementFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<LinkedInPostLink?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedOn:f}",
            ConfigurationFunctionNames.LinkedInProcessNewSpeakingEngagementFired, startedOn);

        if (eventGridEvent.Data is null)
        {
            logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return null;
        }

        try
        {
            var eventGridData = eventGridEvent.Data.ToString();
            var newSpeakingEngagementEvent = JsonSerializer.Deserialize<NewSpeakingEngagementEvent>(eventGridData);
            if (newSpeakingEngagementEvent is null)
            {
                logger.LogError("Failed to parse the data for event '{Id}'", eventGridEvent.Id);
                return null;
            }

            var engagement = await engagementManager.GetAsync(newSpeakingEngagementEvent.Id);
            if (engagement is null)
            {
                logger.LogWarning("Engagement {EngagementId} not found. Skipping.", newSpeakingEngagementEvent.Id);
                return null;
            }

            logger.LogDebug("Processing new speaking engagement '{Id}' with name '{Name}'",
                engagement.Id, LogSanitizer.Sanitize(engagement.Name));

            if (engagement.CreatedByEntraOid is null)
            {
                logger.LogWarning("No owner OID on engagement {Id} — skipping LinkedIn post", engagement.Id);
                return null;
            }

            // Per-user OAuth token — no silent fallback to shared token
            var token = await userOAuthTokenManager.GetByUserAndPlatformAsync(
                engagement.CreatedByEntraOid,
                SocialMediaPlatformIds.LinkedIn);

            if (token is null)
            {
                logger.LogWarning(
                    "No OAuth token found for owner {OwnerOid} on LinkedIn — skipping engagement {Id}",
                    LogSanitizer.Sanitize(engagement.CreatedByEntraOid),
                    engagement.Id);
                return null;
            }

            var scheduledItem = new ScheduledItem
            {
                ItemType = ScheduledItemType.Engagements,
                ItemPrimaryKey = engagement.Id,
                Message = $"New Speaking Engagement: {engagement.Name} {engagement.Url}",
                SendOnDateTime = DateTimeOffset.UtcNow,
                CreatedByEntraOid = engagement.CreatedByEntraOid
            };

            var postText = await linkedInManager.ComposeMessageAsync(scheduledItem);

            if (string.IsNullOrWhiteSpace(postText))
            {
                logger.LogWarning("Composed message was empty for engagement {EngagementId}. Skipping.", engagement.Id);
                return null;
            }

            var properties= new Dictionary<string, string>
            {
                { "post", postText },
                { "name", engagement.Name },
                { "url", engagement.Url },
                { "id", engagement.Id.ToString() }
            };
            logger.LogCustomEvent(Metrics.LinkedInProcessedNewSpeakingEngagement, properties);
            logger.LogDebug("Generated the LinkedIn post for speaking engagement {Id}", engagement.Id);

            return new LinkedInPostLink
            {
                Text = postText,
                Title = engagement.Name,
                LinkUrl = engagement.Url,
                AccessToken = token.AccessToken
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process new speaking engagement. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
        finally
        {
            var endedOn = DateTimeOffset.UtcNow;
            logger.LogDebug("Ended {FunctionName} at {EndedOn:f} with duration {Duration:c}",
                ConfigurationFunctionNames.LinkedInProcessNewSpeakingEngagementFired, endedOn, endedOn - startedOn);
        }
    }
}
