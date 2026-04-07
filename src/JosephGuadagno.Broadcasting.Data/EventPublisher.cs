using Azure;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain.Models.Events;

using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data;

public class EventPublisher(IEventPublisherSettings eventPublisherSettings, ILogger<EventPublisher> logger)
    : IEventPublisher
{
    private const int MaxRetryAttempts = 3;

    /// <summary>
    /// Initial delay between retry attempts. Exposed as protected so tests can override via subclass.
    /// </summary>
    protected TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    public async Task PublishSyndicationFeedEventsAsync(string subject,
        IReadOnlyCollection<SyndicationFeedSource> syndicationFeedSourceDataItems)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }

        if (syndicationFeedSourceDataItems.Count == 0)
        {
            return;
        }

        var topicSettings = GetTopicEndpointSettings(Topics.NewSyndicationFeedItem);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.NewSyndicationFeedItem}' was not found.");
        }

        var client = GetEventGridPublisherClient(topicSettings);

        var eventList = new List<EventGridEvent>();
        foreach (var syndicationFeedDataItem in syndicationFeedSourceDataItems)
        {
            var data = new NewSyndicationFeedItemEvent
            {
                Id = syndicationFeedDataItem.Id
            };
            eventList.Add(
                new EventGridEvent(subject, Topics.NewSyndicationFeedItem, "1.1", data));
        }

        await SendWithRetryAsync(client, eventList, topicSettings.Endpoint, Topics.NewSyndicationFeedItem);
    }

    public async Task PublishYouTubeEventsAsync(string subject, IReadOnlyCollection<YouTubeSource> youTubeSourceDataItems)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }

        if (youTubeSourceDataItems.Count == 0)
        {
            return;
        }

        var topicSettings = GetTopicEndpointSettings(Topics.NewYouTubeItem);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.NewYouTubeItem}' was not found.");
        }

        var client = GetEventGridPublisherClient(topicSettings);

        var eventList = new List<EventGridEvent>();
        foreach (var youTubeSourceDataItem in youTubeSourceDataItems)
        {
            var data = new NewYouTubeItemEvent
            {
                Id = youTubeSourceDataItem.Id
            };
            eventList.Add(
                new EventGridEvent(subject, Topics.NewYouTubeItem, "1.1", data));
        }

        await SendWithRetryAsync(client, eventList, topicSettings.Endpoint, Topics.NewYouTubeItem);
    }


    public async Task PublishSpeakingEngagementEventsAsync(string subject,
        IReadOnlyCollection<Engagement> engagements)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }

        if (engagements.Count == 0)
        {
            return;
        }

        var topicSettings = GetTopicEndpointSettings(Topics.NewSpeakingEngagement);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.NewSpeakingEngagement}' was not found.");
        }

        var client = GetEventGridPublisherClient(topicSettings);

        var eventList = new List<EventGridEvent>();
        foreach (var engagement in engagements)
        {
            var data = new NewSpeakingEngagementEvent
            {
                Id = engagement.Id
            };
            eventList.Add(
                new EventGridEvent(subject, Topics.NewSpeakingEngagement, "1.1", data));
        }

        await SendWithRetryAsync(client, eventList, topicSettings.Endpoint, Topics.NewSpeakingEngagement);
    }

    public async Task PublishScheduledItemFiredEventsAsync(string subject,
        IReadOnlyCollection<ScheduledItem> scheduledItems)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }
            
        if (scheduledItems.Count == 0)
        {
            return;
        }
        
        var topicSettings = GetTopicEndpointSettings(Topics.ScheduledItemFired);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.ScheduledItemFired}' was not found.");
        }

        var client = GetEventGridPublisherClient(topicSettings);

        var eventList = new List<EventGridEvent>();
        foreach (var scheduledItem in scheduledItems)
        {
            var data = new ScheduledItemFiredEvent
            {
                Id = scheduledItem.Id
            };
            eventList.Add(
                new EventGridEvent(subject, Topics.ScheduledItemFired, "1.1", data));
        }

        await SendWithRetryAsync(client, eventList, topicSettings.Endpoint, Topics.ScheduledItemFired);
    }

    public async Task PublishRandomPostsEventsAsync(string subject, int randomPostId)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }
            
        if (randomPostId <= 0)
        {
            return;
        }
        
        var topicSettings = GetTopicEndpointSettings(Topics.NewRandomPost);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.NewRandomPost}' was not found.");
        }

        var client = GetEventGridPublisherClient(topicSettings);

        var data = new RandomPostEvent{ Id = randomPostId};
        var eventList = new List<EventGridEvent>
            { new(subject, Topics.NewRandomPost, "1.0", data) };

        await SendWithRetryAsync(client, eventList, topicSettings.Endpoint, Topics.NewRandomPost);
    }

    private async Task SendWithRetryAsync(
        EventGridPublisherClient client,
        IEnumerable<EventGridEvent> events,
        string topicUrl,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var delay = InitialRetryDelay;

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                await client.SendEventsAsync(events, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                logger.LogWarning(ex,
                    "Event Grid publish attempt {Attempt}/{MaxRetries} failed for event type '{EventType}' to '{TopicUrl}'. Retrying in {DelaySeconds}s.",
                    attempt, MaxRetryAttempts, eventType, topicUrl, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Event Grid publish failed after {MaxRetries} attempts for event type '{EventType}' to '{TopicUrl}'.",
                    MaxRetryAttempts, eventType, topicUrl);
                throw new EventPublishException(
                    $"Failed to publish event type '{eventType}' to '{topicUrl}' after {MaxRetryAttempts} attempts.",
                    ex);
            }
        }
    }

    private ITopicEndpointSettings? GetTopicEndpointSettings(string topicName)
    {
        return eventPublisherSettings.TopicEndpointSettings.FirstOrDefault(t => t.TopicName == topicName);
    }

    protected virtual EventGridPublisherClient GetEventGridPublisherClient(ITopicEndpointSettings topicSettings)
    {
        var topicCredentials = new AzureKeyCredential(topicSettings.Key);
        return new EventGridPublisherClient(new Uri(topicSettings.Endpoint), topicCredentials);
    }
}
