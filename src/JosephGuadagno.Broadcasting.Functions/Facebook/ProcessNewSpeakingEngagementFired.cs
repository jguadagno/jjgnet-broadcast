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
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessNewSpeakingEngagementFired(
    IEngagementManager engagementManager,
    IFacebookManager facebookManager,
    ILogger<ProcessNewSpeakingEngagementFired> logger)
{
    [Function(ConfigurationFunctionNames.FacebookProcessNewSpeakingEngagementFired)]
    [QueueOutput(Queues.FacebookPostStatusToPage)]
    public async Task<FacebookPostStatus?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedOn = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedOn:f}",
            ConfigurationFunctionNames.FacebookProcessNewSpeakingEngagementFired, startedOn);

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

            logger.LogDebug("Processing new speaking engagement '{Id}' with name '{Name}'",
                engagement.Id, LogSanitizer.Sanitize(engagement.Name));

            var scheduledItem = new ScheduledItem
            {
                ItemType = ScheduledItemType.Engagements,
                ItemPrimaryKey = engagement.Id,
                Message = $"New Speaking Engagement: {engagement.Name} {engagement.Url}",
                SendOnDateTime = DateTimeOffset.UtcNow,
                CreatedByEntraOid = engagement.CreatedByEntraOid
            };

            var statusText = await facebookManager.ComposeMessageAsync(scheduledItem);

            var properties = new Dictionary<string, string>
            {
                { "post", statusText },
                { "name", engagement.Name },
                { "url", engagement.Url },
                { "id", engagement.Id.ToString() }
            };
            logger.LogCustomEvent(Metrics.FacebookProcessedNewSpeakingEngagement, properties);
            logger.LogDebug("Generated the Facebook post for speaking engagement {Id}", engagement.Id);

            return new FacebookPostStatus
            {
                StatusText = statusText,
                LinkUri = engagement.Url
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
                ConfigurationFunctionNames.FacebookProcessNewSpeakingEngagementFired, endedOn, endedOn - startedOn);
        }
    }
}
