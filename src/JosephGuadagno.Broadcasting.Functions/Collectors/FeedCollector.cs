using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Utilities.Web.Shortener;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors
{
    public class FeedCollector
    {
        private readonly ISettings _settings;
        private readonly ConfigurationRepository _configurationRepository;
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly Bitly _bitly;

        public FeedCollector(ISettings settings, 
            ConfigurationRepository configurationRepository,
            SourceDataRepository sourceDataRepository,
            Bitly bitly)
        {
            _settings = settings;
            _configurationRepository = configurationRepository;
            _sourceDataRepository = sourceDataRepository;
            _bitly = bitly;
        }
        
        [FunctionName("collector_feed")]
        public async Task RunAsync(
            [TimerTrigger("0 */2 * * * *")] TimerInfo myTimer, 
            ILogger log)
        {
            var startedAt = DateTime.UtcNow;
            log.LogInformation($"C# Timer trigger function executed at: {startedAt}");
            
            var configuration = await _configurationRepository.GetAsync(
                Constants.ConfigurationFunctionNames.CollectorsFeedCollector,
                Constants.Tables.Configuration) ?? new FeedCollectorConfiguration() {LastCheckedFeed = startedAt};
            
            // Check for new items
            log.LogInformation($"Checking '{_settings.FeedUrl}' for posts since '{configuration.LastCheckedFeed}'");
            var feedReader = new FeedReader.FeedReader(_settings.FeedUrl);
            var newItems = feedReader.Get(configuration.LastCheckedFeed);
            
            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                configuration.LastCheckedFeed = startedAt;
                await _configurationRepository.SaveAsync(configuration);
                log.LogDebug($"No new post found at '{_settings.FeedUrl}'.");
                return;
            }
            
            // Save the new items to SourceDataRepository
            foreach (var item in newItems)
            {
                item.ShortenedUrl = await GetShortenedUrlAsync(item.Url);
            }
            await _sourceDataRepository.AddAllAsync(newItems);
            
            // Send the events 
            await PublishEvents(_settings.TopicNewSourceDataEndpoint, _settings.TopicNewSourceDataKey, newItems);
            
            // Save the last checked value
            configuration.LastCheckedFeed = startedAt;
            await _configurationRepository.SaveAsync(configuration);
            log.LogDebug("Done.");
        }
        
        private async Task<string> GetShortenedUrlAsync(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl))
            {
                return null;
            }

            var result = await _bitly.Shorten(originalUrl, "jjg.me");
            return result == null ? originalUrl : result.Link;
        }

        private async Task<bool> PublishEvents(string topicUrl, string topicKey, List<SourceData> sourceDataItems)
        {
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
                        Subject = Constants.ConfigurationFunctionNames.CollectorsFeedCollector,
                        DataVersion = "1.0"
                    });
            }

            await client.PublishEventsAsync(topicHostName, eventList);

            return true;
        }
    }
}