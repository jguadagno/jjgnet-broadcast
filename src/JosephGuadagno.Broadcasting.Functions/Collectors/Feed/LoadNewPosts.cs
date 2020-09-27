using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.FeedReader;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.Feed
{
    public class CheckFeedForUpdates
    {
        private readonly IFeedReader _feedReader;
        private readonly ISettings _settings;
        private readonly ConfigurationRepository _configurationRepository;
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly IUrlShortener _urlShortener;
        private readonly IEventPublisher _eventPublisher;

        public CheckFeedForUpdates(IFeedReader feedReader,
            ISettings settings, 
            ConfigurationRepository configurationRepository,
            SourceDataRepository sourceDataRepository,
            IUrlShortener urlShortener, 
            IEventPublisher eventPublisher)
        {
            _feedReader = feedReader;
            _settings = settings;
            _configurationRepository = configurationRepository;
            _sourceDataRepository = sourceDataRepository;
            _urlShortener = urlShortener;
            _eventPublisher = eventPublisher;
        }
        
        [FunctionName("collectors_feed_check_for_updates")]
        public async Task RunAsync(
            [TimerTrigger("0 */2 * * * *")] TimerInfo myTimer,
            ILogger log)
        {
            var startedAt = DateTime.UtcNow;
            log.LogDebug($"{Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts} Collector started at: {startedAt}");

            var configuration = await _configurationRepository.GetAsync(Constants.Tables.Configuration,
                                    Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts
                                ) ??
                                new CollectorConfiguration(Constants.ConfigurationFunctionNames
                                        .CollectorsFeedLoadNewPosts)
                                    {LastCheckedFeed = startedAt};
            
            // Check for new items
            log.LogDebug($"Checking '{_settings.FeedUrl}' for posts since '{configuration.LastCheckedFeed}'");
            var newItems = await _feedReader.GetAsync(configuration.LastCheckedFeed);
            
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
            // GitHub Issue #5
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // shorten the url
                item.ShortenedUrl = await _urlShortener.GetShortenedUrlAsync(item.Url, "jjg.me");
                
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
            var eventsPublished = await _eventPublisher.PublishEventsAsync(_settings.TopicNewSourceDataEndpoint, _settings.TopicNewSourceDataKey,
                Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, newItems);

            if (!eventsPublished)
            {
                log.LogError("Failed to publish the events.");
            }
            
            // Save the last checked value
            configuration.LastCheckedFeed = startedAt;
            await _configurationRepository.SaveAsync(configuration);
            
            // Return
            var doneMessage = $"Loaded {savedCount} of {newItems.Count} post(s).";
            log.LogInformation(doneMessage);
        }
    }
}