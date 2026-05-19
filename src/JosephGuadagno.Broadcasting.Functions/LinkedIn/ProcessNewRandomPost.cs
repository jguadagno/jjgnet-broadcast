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

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessNewRandomPost(
    ISyndicationFeedItemManager syndicationFeedItemManager,
    IMessageTemplateManager messageTemplateManager,
    IPostComposer postComposer,
    ILogger<ProcessNewRandomPost> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInProcessRandomPostFired)]
    [QueueOutput(Queues.LinkedInPostLink)]
    public async Task<SocialMediaPublishRequest?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
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
            var syndicationFeedItem = await syndicationFeedItemManager.GetAsync(source.Id);

            var ownerEntraOid = syndicationFeedItem.CreatedByEntraOid;
            if (string.IsNullOrEmpty(ownerEntraOid))
            {
                logger.LogWarning("No owner OID for syndication item {Id} — skipping LinkedIn random post",
                    syndicationFeedItem.Id);
                return null;
            }

            var request = new SocialMediaPublishRequest
            {
                Text = "",
                Title = syndicationFeedItem.Title,
                LinkUrl = syndicationFeedItem.Url,
                ShortenedUrl = syndicationFeedItem.ShortenedUrl,
                Hashtags = syndicationFeedItem.Tags.Count > 0 ? syndicationFeedItem.Tags.ToList() : null,
                OwnerEntraOid = ownerEntraOid
            };

            var template = await messageTemplateManager.GetAsync(
                MessageTemplates.Platforms.LinkedIn,
                MessageTemplates.MessageTypes.RandomPost,
                ownerEntraOid);
            if (template is null)
                return null;

            var composedText = await postComposer.ComposeAsync(request, template.Template);
            if (string.IsNullOrWhiteSpace(composedText))
            {
                logger.LogWarning("Compose returned empty for random post item {Id}", syndicationFeedItem.Id);
                return null;
            }

            var properties = new Dictionary<string, string>
            {
                {"title", syndicationFeedItem.Title},
                {"url", syndicationFeedItem.Url},
                {"post", composedText}
            };
            logger.LogCustomEvent(Metrics.LinkedInProcessedRandomPost, properties);
            logger.LogDebug("Picked a random post {Title}", syndicationFeedItem.Title);

            request.Text = composedText;
            return request;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process the new random post. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}