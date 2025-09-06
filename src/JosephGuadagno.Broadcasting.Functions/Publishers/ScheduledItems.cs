using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class ScheduledItems
{
    private readonly IScheduledItemManager _scheduledItemManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly ConfigurationRepository _configurationRepository;
    private readonly ISettings _settings;
    private readonly ILogger<ScheduledItems> _logger;
    private readonly TelemetryClient _telemetryClient;

    public ScheduledItems(IScheduledItemManager scheduledItemManager,
        IEventPublisher eventPublisher,
        ConfigurationRepository configurationRepository,
        ISettings settings,
        ILogger<ScheduledItems> logger,
        TelemetryClient telemetryClient)
    {
        _scheduledItemManager = scheduledItemManager;
        _eventPublisher = eventPublisher;
        _configurationRepository = configurationRepository;
        _settings = settings;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
    
    [Function("publishers_scheduled_items")]
    public async Task RunAsync([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer, ILogger log)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.PublishersScheduledItems, startedAt);

        var configuration = await _configurationRepository.GetAsync(Constants.Tables.Configuration,
                                Constants.ConfigurationFunctionNames.PublishersScheduledItems
                            ) ??
                            new CollectorConfiguration(Constants.ConfigurationFunctionNames
                                    .PublishersScheduledItems)
                                {LastCheckedFeed = startedAt, LastItemAddedOrUpdated = DateTime.MinValue};

        // Check for items that are due to be fired
        _logger.LogDebug("Checking for scheduled items that have not been fired");
        var scheduledItems =
            await _scheduledItemManager.GetScheduledItemsToSendAsync();

        // If there are no scheduled items, log it, and exit
        if (scheduledItems is null || scheduledItems.Count == 0)
        {
            configuration.LastCheckedFeed = startedAt;
            await _configurationRepository.SaveAsync(configuration);
            _logger.LogDebug("No new scheduled items found");
            return;
        }
        
        // Iterate through the scheduled item
        foreach (var scheduledItem in scheduledItems)
        {
            _telemetryClient.TrackEvent(Constants.Metrics.ScheduledItemFired, scheduledItem.ToDictionary());
        }
        
        // Publish the events
        var eventsPublished = await _eventPublisher.PublishEventsAsync(_settings.TopicScheduledItemFiredDataEndpoint,
            _settings.TopicScheduledItemFiredDataKey,
            Constants.ConfigurationFunctionNames.PublishersScheduledItems, scheduledItems);
        if (!eventsPublished)
        {
            _logger.LogError("Failed to publish the events for some scheduled items");
        }
        else
        {
            // Mark the messages as sent
            foreach (var scheduledItem in scheduledItems)
            {
                var wasSent = await _scheduledItemManager.SentScheduledItemAsync(scheduledItem.Id);
                if (!wasSent)
                {
                    _logger.LogWarning(
                        "Failed to update the sent flag for scheduled items with the id of '{ScheduledItemId}'",
                        scheduledItem.Id);
                }
            }
        }

        // Save the last checked value
        configuration.LastCheckedFeed = startedAt;
        await _configurationRepository.SaveAsync(configuration);
        
        _logger.LogDebug("Done publishing the events for schedule items");
    }
}