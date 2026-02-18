using Azure;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain.Models.Events;

using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data;

public class EventPublisher(IEventPublisherSettings eventPublisherSettings, ILogger<EventPublisher> logger)
    : IEventPublisher
{
    public async Task<bool> PublishSyndicationFeedEventsAsync(string subject,
        IReadOnlyCollection<SyndicationFeedSource> syndicationFeedSourceDataItems)
    {

        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }

        if (syndicationFeedSourceDataItems.Count == 0)
        {
            return false;
        }

        var topicSettings = GetTopicEndpointSettings(Topics.NewSyndicationFeedItem);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.NewSyndicationFeedItem}' was not found.");
        }

        var topicCredentials = new AzureKeyCredential(topicSettings.Key);
        var client= new EventGridPublisherClient(new Uri(topicSettings.Endpoint), topicCredentials);

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

        try
        {
            await client.SendEventsAsync(eventList);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to publish the event to TopicUrl: '{TopicUrl}'. Exception: '{Exception}'", topicSettings.Endpoint, e);
            return false;
        }
    }

    public async Task<bool> PublishYouTubeEventsAsync(string subject, IReadOnlyCollection<YouTubeSource> youTubeSourceDataItems)
    {

        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }

        if (youTubeSourceDataItems.Count == 0)
        {
            return false;
        }

        var topicSettings = GetTopicEndpointSettings(Topics.NewYouTubeItem);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.NewYouTubeItem}' was not found.");
        }

        var topicCredentials = new AzureKeyCredential(topicSettings.Key);
        var client= new EventGridPublisherClient(new Uri(topicSettings.Endpoint), topicCredentials);

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

        try
        {
            await client.SendEventsAsync(eventList);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to publish the event to TopicUrl: '{TopicUrl}'. Exception: '{Exception}'", topicSettings.Endpoint, e);
            return false;
        }
    }

    public async Task<bool> PublishScheduledItemFiredEventsAsync(string subject,
        IReadOnlyCollection<ScheduledItem> scheduledItems)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }
            
        if (scheduledItems.Count == 0)
        {
            return false;
        }
        
        var topicSettings = GetTopicEndpointSettings(Topics.ScheduledItemFired);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.ScheduledItemFired}' was not found.");
        }

        var topicCredentials = new AzureKeyCredential(topicSettings.Key);
        var client= new EventGridPublisherClient(new Uri(topicSettings.Endpoint), topicCredentials);

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
        
        try
        {
            await client.SendEventsAsync(eventList);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to publish the event to TopicUrl: '{TopicUrl}'. Exception: '{Exception}'", topicSettings.Endpoint, e);
            return false;
        }
    }

    public async Task<bool> PublishRandomPostsEventsAsync(string subject, int randomPostId)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }
            
        if (randomPostId <= 0)
        {
            return false;
        }
        
        var topicSettings = GetTopicEndpointSettings(Topics.NewRandomPost);
        if (topicSettings == null)
        {
            throw new InvalidOperationException($"The topic endpoint settings for topic '{Topics.NewRandomPost}' was not found.");
        }

        var topicCredentials = new AzureKeyCredential(topicSettings.Key);
        var client= new EventGridPublisherClient(new Uri(topicSettings.Endpoint), topicCredentials);

        var data = new RandomPostEvent{ Id = randomPostId};

        var eventList = new List<EventGridEvent>
            { new(subject, Topics.NewRandomPost, "1.0", data) };

        try
        {
            await client.SendEventsAsync(eventList);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to publish the event to TopicUrl: '{TopicUrl}'. Exception: '{Exception}'", topicSettings.Endpoint, e);
            return false;
        }
    }

    private ITopicEndpointSettings? GetTopicEndpointSettings(string topicName)
    {
        return eventPublisherSettings.TopicEndpointSettings.FirstOrDefault(t => t.TopicName == topicName);
    }
}