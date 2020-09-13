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
    public class CheckFeedForUpdates
    {
        private readonly ISettings _settings;
        private readonly ConfigurationRepository _configurationRepository;
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly Bitly _bitly;

        public CheckFeedForUpdates(ISettings settings, 
            ConfigurationRepository configurationRepository,
            SourceDataRepository sourceDataRepository,
            Bitly bitly)
        {
            _settings = settings;
            _configurationRepository = configurationRepository;
            _sourceDataRepository = sourceDataRepository;
            _bitly = bitly;
        }
        
        [FunctionName("collectors_feed_check_for_updates")]
        public async Task RunAsync(
            [TimerTrigger("0 */2 * * * *")] 
            ILogger log)
        {
            var startedAt = DateTime.UtcNow;
            log.LogDebug($"{Constants.ConfigurationFunctionNames.CollectorsCheckForFeedUpdates} Collector started at: {startedAt}");
            
            var configuration = await _configurationRepository.GetAsync(
                Constants.ConfigurationFunctionNames.CollectorsCheckForFeedUpdates,
                Constants.Tables.Configuration) ?? new FeedCollectorConfiguration() {LastCheckedFeed = startedAt};
            
            // Check for new items
            log.LogDebug($"Checking '{_settings.FeedUrl}' for posts since '{configuration.LastCheckedFeed}'");
            var feedReader = new FeedReader.FeedReader(_settings.FeedUrl);
            var newItems = feedReader.Get(configuration.LastCheckedFeed);
            
            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                configuration.LastCheckedFeed = startedAt;
                await _configurationRepository.SaveAsync(configuration);
                log.LogDebug($"No new posts found at '{_settings.FeedUrl}'.");
                return;
            }
            
            // Save the new items to SourceDataRepository
            // TODO: Handle duplicate posts?
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // shorten the url
                item.ShortenedUrl = await GetShortenedUrlAsync(item.Url);
                
                // attempt to save the item
                var saveWasSuccessful = false;
                try
                {
                    saveWasSuccessful = await _sourceDataRepository.SaveAsync(item);
                }
                catch (Exception e)
                {
                    log.LogError($"Was not able to save post with the id of '{item.Id}'. Exception: {e.Message}");
                }
                
                if (!saveWasSuccessful)
                {
                    log.LogError($"Was not able to save post with the id of '{item.Id}'.");
                }
                else
                {
                    savedCount++;
                }
            }
            
            // Send the events 
            await PublishEvents(_settings.TopicNewSourceDataEndpoint, _settings.TopicNewSourceDataKey, newItems);
            
            // Save the last checked value
            configuration.LastCheckedFeed = startedAt;
            await _configurationRepository.SaveAsync(configuration);
            
            // Return
            var doneMessage = $"Loaded {savedCount} of {newItems.Count} post(s).";
            log.LogInformation(doneMessage);
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
                        Subject = Constants.ConfigurationFunctionNames.CollectorsCheckForFeedUpdates,
                        DataVersion = "1.0"
                    });
            }

            await client.PublishEventsAsync(topicHostName, eventList);

            return true;
        }
    }
}