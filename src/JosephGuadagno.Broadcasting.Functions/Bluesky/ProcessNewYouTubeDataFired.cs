using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class ProcessNewYouTubeDataFired(
    IYouTubeItemManager youTubeItemManager,
    IMessageTemplateManager messageTemplateManager,
    IPostComposer postComposer,
    ILogger<ProcessNewYouTubeDataFired> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyProcessNewYouTubeDataFired)]
    [QueueOutput(Queues.BlueskyPostToSend)]
    public async Task<SocialMediaPublishRequest?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        try
        {
            var startedAt = DateTimeOffset.UtcNow;
            logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
                ConfigurationFunctionNames.BlueskyProcessNewYouTubeDataFired, startedAt);

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

            logger.LogDebug("Processing New YouTube Feed Data Fired for '{Id}' with title of '{Title}'",
                youTubeItem.Id, youTubeItem.Title);

            var ownerEntraOid = youTubeItem.CreatedByEntraOid;
            if (string.IsNullOrEmpty(ownerEntraOid))
            {
                logger.LogWarning("No owner OID for YouTube item {Id} — skipping Bluesky post", youTubeItem.Id);
                return null;
            }

            var request = new SocialMediaPublishRequest
            {
                Text = "",
                Title = youTubeItem.Title,
                LinkUrl = youTubeItem.Url,
                ShortenedUrl = youTubeItem.ShortenedUrl,
                Hashtags = youTubeItem.Tags.Count > 0 ? youTubeItem.Tags.ToList() : null,
                OwnerEntraOid = ownerEntraOid
            };

            var template = await messageTemplateManager.GetAsync(
                MessageTemplates.Platforms.Bluesky,
                MessageTemplates.MessageTypes.NewYouTubeItem,
                ownerEntraOid);
            if (template is null)
                return null;

            var composedText = await postComposer.ComposeAsync(request, template.Template);
            if (string.IsNullOrWhiteSpace(composedText))
            {
                logger.LogWarning("Compose returned empty for YouTube item {Id}", youTubeItem.Id);
                return null;
            }

            var properties = new Dictionary<string, string>
            {
                {"post", composedText},
                {"title", youTubeItem.Title},
                {"url", youTubeItem.Url},
                {"id", youTubeItem.Id.ToString()}
            };
            logger.LogCustomEvent(Metrics.BlueskyProcessedNewYouTubeData, properties);
            logger.LogDebug("Posted to Bluesky: {Title}", youTubeItem.Title);

            request.Text = composedText;
            return request;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process the new YouTube data. Exception: {ExceptionMessage}", exception.Message);
            return null;
        }
    }
}