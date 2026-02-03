using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.Feed;

public class LoadNewPosts
{
    private readonly ISyndicationFeedReader _syndicationFeedReader;
    private readonly ISettings _settings;
    private readonly ConfigurationRepository _configurationRepository;
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly IUrlShortener _urlShortener;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<LoadNewPosts> _logger;
    private readonly TelemetryClient _telemetryClient;

    public LoadNewPosts(ISyndicationFeedReader syndicationFeedReader,
        ISettings settings,
        ConfigurationRepository configurationRepository,
        SourceDataRepository sourceDataRepository,
        IUrlShortener urlShortener,
        IEventPublisher eventPublisher,
        ILogger<LoadNewPosts> logger,
        TelemetryClient telemetryClient)
    {
        _syndicationFeedReader = syndicationFeedReader;
        _settings = settings;
        _configurationRepository = configurationRepository;
        _sourceDataRepository = sourceDataRepository;
        _urlShortener = urlShortener;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
        
    [Function(Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts)]
    public async Task RunAsync(
        [TimerTrigger("%collectors_feed_load_new_posts_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, startedAt);

        var configuration = await _configurationRepository.GetAsync(Constants.Tables.Configuration,
                                Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts
                            ) ??
                            new CollectorConfiguration(Constants.ConfigurationFunctionNames
                                    .CollectorsFeedLoadNewPosts)
                                {LastCheckedFeed = startedAt, LastItemAddedOrUpdated = DateTime.MinValue};
            
        // Check for new items
        _logger.LogDebug("Checking the syndication feed for posts since '{LastItemAddedOrUpdated}'", configuration.LastItemAddedOrUpdated);
        var newItems = await _syndicationFeedReader.GetAsync(configuration.LastItemAddedOrUpdated);
            
        // If there is nothing new, save the last checked value and exit
        if (newItems == null || newItems.Count == 0)
        {
            configuration.LastCheckedFeed = startedAt;
            await _configurationRepository.SaveAsync(configuration);
            _logger.LogDebug("No new or updated posts found in the syndication feed");
            return;
        }
            
        // Save the new items to SourceDataRepository
        var savedCount = 0;
        var eventsToPublish = new List<SourceData>();
        foreach (var item in newItems)
        {
            // shorten the url
            item.ShortenedUrl = await _urlShortener.GetShortenedUrlAsync(item.Url, _settings.BitlyShortenedDomain);

            // attempt to save the item
            try
            {
                var wasSaved = await _sourceDataRepository.SaveAsync(item);
                if (wasSaved)
                {
                    eventsToPublish.Add(item);
                    _telemetryClient.TrackEvent(Constants.Metrics.PostAddedOrUpdated, item.ToDictionary());
                    savedCount++;
                }
                else
                {
                    _logger.LogError("Failed to save the blog post with the id of: '{Id}' Url:'{Url}'",
                        item.Id, item.Url);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to save the blog post with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                    item.Id, item.Url, e);
            }
        }

        // Publish the events
        var eventsPublished = await _eventPublisher.PublishEventsAsync(_settings.TopicNewSourceDataEndpoint,
            _settings.TopicNewSourceDataKey,
            Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, eventsToPublish);
        if (!eventsPublished)
        {
            _logger.LogError("Failed to publish the events for the new or updated blog posts");
        }
            
        // Save the last checked value
        configuration.LastCheckedFeed = startedAt;
        var latestAdded = newItems.Max(item => item.PublicationDate);
        var latestUpdated = newItems.Max(item => item.UpdatedOnDate);
        configuration.LastItemAddedOrUpdated = latestUpdated > latestAdded
            ? latestUpdated.Value.ToUniversalTime()
            : latestAdded.ToUniversalTime();

        await _configurationRepository.SaveAsync(configuration);
            
        // Return
        _logger.LogInformation("Loaded {SavedCount} of {TotalPostsCount} post(s)", savedCount, newItems.Count);
    }
}