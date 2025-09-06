using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;

public class LoadNewVideos
{
    private readonly IYouTubeReader _youTubeReader;
    private readonly ISettings _settings;
    private readonly ConfigurationRepository _configurationRepository;
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly IUrlShortener _urlShortener;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<LoadNewVideos> _logger;
    private readonly TelemetryClient _telemetryClient;

    public LoadNewVideos(IYouTubeReader youTubeReader,
        ISettings settings, 
        ConfigurationRepository configurationRepository,
        SourceDataRepository sourceDataRepository,
        IUrlShortener urlShortener,
        IEventPublisher eventPublisher,
        ILogger<LoadNewVideos> logger,
        TelemetryClient telemetryClient)
    {
        _youTubeReader = youTubeReader;
        _settings = settings;
        _configurationRepository = configurationRepository;
        _sourceDataRepository = sourceDataRepository;
        _urlShortener = urlShortener;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
        
    [Function(Constants.ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos)]
    public async Task RunAsync(
        [TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos, startedAt);

        var configuration = await _configurationRepository.GetAsync(
                                Constants.Tables.Configuration,
                                Constants.ConfigurationFunctionNames.CollectorsYouTubeLoadNewVideos) ??
                            new CollectorConfiguration(Constants.ConfigurationFunctionNames
                                    .CollectorsYouTubeLoadNewVideos)
                                {LastCheckedFeed = startedAt, LastItemAddedOrUpdated = DateTime.MinValue};
            
        // Check for new items
        _logger.LogDebug("Checking playlist for videos since '{LastItemAddedOrUpdated}'",
            configuration.LastItemAddedOrUpdated);
        var newItems = await _youTubeReader.GetAsync(configuration.LastItemAddedOrUpdated);
            
        // If there is nothing new, save the last checked value and exit
        if (newItems == null || newItems.Count == 0)
        {
            configuration.LastCheckedFeed = startedAt;
            await _configurationRepository.SaveAsync(configuration);
            _logger.LogDebug("No new videos found in the playlist");
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
                    _telemetryClient.TrackEvent(Constants.Metrics.VideoAddedOrUpdated, item.ToDictionary());
                    savedCount++;
                }
                else
                {
                    _logger.LogError("Failed to save the video with the id of: '{Id}' Url:'{Url}'",
                        item.Id, item.Url);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to save the video with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                    item.Id, item.Url, e);
            }
        }

        // Publish the events

        var eventsPublished = await _eventPublisher.PublishEventsAsync(_settings.TopicNewSourceDataEndpoint,
            _settings.TopicNewSourceDataKey,
            Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, eventsToPublish);
        if (!eventsPublished)
        {
            _logger.LogError("Failed to publish the events for the new or updated videos");
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
        _logger.LogInformation("Loaded {SavedCount} of {TotalVideoCount} video(s)", savedCount, newItems.Count);
    }
}