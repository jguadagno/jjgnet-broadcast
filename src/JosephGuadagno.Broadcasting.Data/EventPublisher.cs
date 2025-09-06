using Azure;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data;

public class EventPublisher: IEventPublisher
{

    private readonly ILogger<EventPublisher> _logger;
        
    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PublishEventsAsync(string topicUrl, string topicKey, string subject, IReadOnlyCollection<SourceData> sourceDataItems)
    {
            
        if (string.IsNullOrEmpty(topicUrl))
        {
            throw new ArgumentNullException(nameof(topicUrl), "The topic url is required.");
        }

        if (string.IsNullOrEmpty(topicKey))
        {
            throw new ArgumentNullException(nameof(topicKey), "The topic key is required.");
        }
            
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }
            
        if (sourceDataItems == null || sourceDataItems.Count == 0)
        {
            return false;
        }
            
        var topicCredentials = new AzureKeyCredential(topicKey);
        var client= new EventGridPublisherClient(new Uri(topicUrl), topicCredentials);

        var eventList = new List<EventGridEvent>();
        foreach (var sourceData in sourceDataItems)
        {
            var data = new TableEvent
            {
                TableName = Constants.Tables.SourceData, 
                PartitionKey = sourceData.PartitionKey,
                RowKey = sourceData.RowKey
            };
            eventList.Add(
                new EventGridEvent(subject, Constants.Topics.NewSourceData, "1.0", data));
        }

        try
        {
            await client.SendEventsAsync(eventList);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to publish the event to TopicUrl: '{TopicUrl}'. Exception: '{Exception}'", topicUrl, e);   
            return false;
        }
    }

    public async Task<bool> PublishEventsAsync(string topicUrl, string topicKey, string subject,
        IReadOnlyCollection<ScheduledItem> scheduledItems)
    {
        if (string.IsNullOrEmpty(topicUrl))
        {
            throw new ArgumentNullException(nameof(topicUrl), "The topic url is required.");
        }

        if (string.IsNullOrEmpty(topicKey))
        {
            throw new ArgumentNullException(nameof(topicKey), "The topic key is required.");
        }
            
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }
            
        if (scheduledItems == null || scheduledItems.Count == 0)
        {
            return false;
        }
        
        var topicCredentials = new AzureKeyCredential(topicKey);
        var client= new EventGridPublisherClient(new Uri(topicUrl), topicCredentials);

        var eventList = new List<EventGridEvent>();
        foreach (var scheduledItem in scheduledItems)
        {
            var data = new TableEvent
            {
                TableName = scheduledItem.ItemTableName, 
                PartitionKey = scheduledItem.ItemPrimaryKey,
                RowKey = scheduledItem.ItemSecondaryKey
            };
            eventList.Add(
                new EventGridEvent(subject, Constants.Topics.ScheduledItemFired, "1.0", data));
        }
        
        try
        {
            await client.SendEventsAsync(eventList);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to publish the event to TopicUrl: '{TopicUrl}'. Exception: '{Exception}'", topicUrl, e);   
            return false;
        }
    }

    public async Task<bool> PublishEventsAsync(string topicUrl, string topicKey, string subject, string randomPostId)
    {
        if (string.IsNullOrEmpty(topicUrl))
        {
            throw new ArgumentNullException(nameof(topicUrl), "The topic url is required.");
        }

        if (string.IsNullOrEmpty(topicKey))
        {
            throw new ArgumentNullException(nameof(topicKey), "The topic key is required.");
        }
            
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentNullException(nameof(subject), "The subject is required.");
        }
            
        if (string.IsNullOrEmpty(randomPostId))
        {
            return false;
        }
        
        var topicCredentials = new AzureKeyCredential(topicKey);
        var client= new EventGridPublisherClient(new Uri(topicUrl), topicCredentials);

        var eventList = new List<EventGridEvent>
            { new(subject, Constants.Topics.NewRandomPost, "1.0", randomPostId) };

        try
        {
            await client.SendEventsAsync(eventList);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to publish the event to TopicUrl: '{TopicUrl}'. Exception: '{Exception}'", topicUrl, e);   
            return false;
        }
    }
}