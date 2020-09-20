using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;

namespace JosephGuadagno.Broadcasting.Data
{
    public class EventPublisher: IEventPublisher
    {
        public bool PublishEvents(string topicUrl, string topicKey, string subject, IReadOnlyCollection<SourceData> sourceDataItems)
        {
            return PublishEventsAsync(topicUrl, topicKey, subject, sourceDataItems).Result;
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
            
            var topicHostName = new Uri(topicUrl).Host;
            var topicCredentials = new TopicCredentials(topicKey);
            var client= new EventGridClient(topicCredentials);

            var eventList = new List<EventGridEvent>();
            foreach (var sourceData in sourceDataItems)
            {
                eventList.Add(
                    new EventGridEvent
                    {
                        Id = sourceData.RowKey,
                        EventType= Constants.Topics.NewSourceData,
                        Data = new TableEvent
                        {
                            TableName = Constants.Tables.SourceData, 
                            PartitionKey = sourceData.PartitionKey,
                            RowKey = sourceData.RowKey
                        },
                        EventTime = DateTime.UtcNow,
                        Subject = subject,
                        DataVersion = "1.0"
                    });
            }

            await client.PublishEventsAsync(topicHostName, eventList);

            return true;
        }
    }
}