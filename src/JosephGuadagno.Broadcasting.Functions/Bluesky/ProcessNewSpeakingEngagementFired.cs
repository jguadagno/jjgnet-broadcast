using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessNewSpeakingEngagementFired(
    IEngagementManager engagementManager,
    IMessageTemplateLookup messageLookup,
    IPostComposer postComposer,
    ILogger<ProcessNewSpeakingEngagementFired> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyProcessNewSpeakingEngagementFired)]
    [QueueOutput(Queues.BlueskyPostToSend)]
    public async Task<BlueskyPostMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedOn:f}",
            ConfigurationFunctionNames.BlueskyProcessNewSpeakingEngagementFired, startedOn);

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

            var ownerEntraOid = engagement.CreatedByEntraOid;
            if (string.IsNullOrEmpty(ownerEntraOid))
            {
                logger.LogWarning("No owner OID on engagement {Id} — skipping Bluesky post", engagement.Id);
                return null;
            }

            var request = new SocialMediaPublishRequest
            {
                Text = "",
                Title = engagement.Name,
                LinkUrl = engagement.Url,
                OwnerEntraOid = ownerEntraOid
            };

            var template = await messageLookup.GetAsync(
                MessageTemplates.Platforms.Bluesky,
                MessageTemplates.MessageTypes.NewSpeakingEngagement,
                ownerEntraOid);
            if (template is null)
                return null;

            var composedText = await postComposer.ComposeAsync(request, template.Template);
            if (string.IsNullOrWhiteSpace(composedText))
            {
                logger.LogWarning("Compose returned empty for speaking engagement {Id}", engagement.Id);
                return null;
            }

            var properties = new Dictionary<string, string>
            {
                { "post", composedText },
                { "name", engagement.Name },
                { "url", engagement.Url },
                { "id", engagement.Id.ToString() }
            };
            logger.LogCustomEvent(Metrics.BlueskyProcessedNewSpeakingEngagement, properties);
            logger.LogDebug("Generated the Bluesky post for speaking engagement {Id}", engagement.Id);

            return new BlueskyPostMessage
            {
                Text = composedText,
                Url = engagement.Url,
                CreatedByEntraOid = ownerEntraOid
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
                ConfigurationFunctionNames.BlueskyProcessNewSpeakingEngagementFired, endedOn, endedOn - startedOn);
        }
    }
}
