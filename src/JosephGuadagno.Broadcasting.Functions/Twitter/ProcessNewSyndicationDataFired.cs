using System.Text.Json;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Twitter;

public class ProcessNewSyndicationDataFired(
    ISyndicationFeedItemManager syndicationFeedItemManager,
    IMessageTemplateLookup messageLookup,
    IPostComposer postComposer,
    ILogger<ProcessNewSyndicationDataFired> logger)
{
    [Function(ConfigurationFunctionNames.TwitterProcessNewSyndicationDataFired)]
    [QueueOutput(Queues.TwitterTweetsToSend)]
    public async Task<TwitterTweetMessage?> RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.TwitterProcessNewSyndicationDataFired, startedAt);

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
        var syndicationFeedItem = await syndicationFeedItemManager.GetAsync(syndicationFeedItemEvent.Id);

        logger.LogDebug("Composing tweet for '{Id}' with title of '{Title}'",
            syndicationFeedItem.Id, syndicationFeedItem.Title);

        var ownerEntraOid = syndicationFeedItem.CreatedByEntraOid;
        if (string.IsNullOrEmpty(ownerEntraOid))
        {
            logger.LogWarning("No owner OID for syndication item {Id} — skipping Twitter post",
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

        var template = await messageLookup.GetAsync(
            MessageTemplates.Platforms.Twitter,
            MessageTemplates.MessageTypes.NewSyndicationFeedItem,
            ownerEntraOid);
        if (template is null)
            return null;

        var composedText = await postComposer.ComposeAsync(request, template.Template);
        if (string.IsNullOrWhiteSpace(composedText))
        {
            logger.LogWarning("Compose returned empty for syndication item {Id}", syndicationFeedItem.Id);
            return null;
        }

        var properties = new Dictionary<string, string>
        {
            {"post", composedText},
            {"title", syndicationFeedItem.Title},
            {"url", syndicationFeedItem.Url},
            {"id", syndicationFeedItem.Id.ToString()}
        };
        logger.LogCustomEvent(Metrics.TwitterProcessedNewSyndicationData, properties);
        logger.LogDebug("Done composing Twitter tweet for '{Id}' with title of '{Title}'",
            syndicationFeedItem.Id, syndicationFeedItem.Title);

        return new TwitterTweetMessage { Text = composedText, CreatedByEntraOid = ownerEntraOid };
    }
}