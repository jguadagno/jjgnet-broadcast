using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class RandomPosts
{
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISettings _settings;
    private readonly ILogger<RandomPosts> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IRandomPostSettings _randomPostSettings;

    public RandomPosts(SourceDataRepository sourceDataRepository,
        IRandomPostSettings randomPostSettings,
        IEventPublisher eventPublisher,
        ISettings settings,
        ILogger<RandomPosts> logger,
        TelemetryClient telemetryClient)
    {
        _sourceDataRepository = sourceDataRepository;
        _randomPostSettings = randomPostSettings;
        _eventPublisher = eventPublisher;
        _settings = settings;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
     
    // TODO: Change this to an event and add a Twitter, and Bluesky Publisher... Maybe LinkedIn/Facebook?
    [Function(Constants.ConfigurationFunctionNames.PublishersRandomPosts)]
    public async Task Run(
        [TimerTrigger("%publishers_random_post_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.PublishersRandomPosts, startedAt);

        // Get the feed items
        // Check for the from date
        var cutoffDate = DateTime.MinValue;
        if (_randomPostSettings.CutoffDate != DateTime.MinValue)
        {
            cutoffDate = _randomPostSettings.CutoffDate;
        }

        _logger.LogDebug("Getting all items from feed from '{CutoffDate:u}'", cutoffDate);
        var randomSourceData = await _sourceDataRepository.GetRandomSourceDataAsync(cutoffDate);

        if (randomSourceData is null)
        {
            _logger.LogDebug("Could not find a random post from feed since '{CutoffDate:u}'", cutoffDate);
            return;
        }
        
        // Create the event message to post to the topic
        var eventPublished = await _eventPublisher.PublishEventsAsync(_settings.TopicNewRandomPostEndpoint,
            _settings.TopicNewRandomPostKey, Constants.ConfigurationFunctionNames.PublishersRandomPosts,
            randomSourceData.Id);
        if (!eventPublished)
        {
            _logger.LogError("Failed to publish the events for the random posts");
            return;
        }
        
        _telemetryClient.TrackEvent(Constants.Metrics.RandomPostFired, new Dictionary<string, string>
        {
            {"title", randomSourceData.Title}, 
            {"url", randomSourceData.Url},
            {"sourceSystem", randomSourceData.SourceSystem},
            {"partitionKey", randomSourceData.PartitionKey},
            {"rowKey", randomSourceData.RowKey},
        });
        
        _logger.LogDebug("Latest random post '{RandomSyndicationIdTitleText}' has been published", randomSourceData.Title);
    }
}