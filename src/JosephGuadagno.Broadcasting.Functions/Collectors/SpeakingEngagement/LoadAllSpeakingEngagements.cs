using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.SpeakingEngagement;

public class LoadAllSpeakingEngagements
{
    private readonly ISpeakingEngagementsReader _speakerEngagementsReader;
    private readonly IEngagementRepository _engagementRepository;
    private readonly ILogger<LoadAllSpeakingEngagements> _logger;
    private readonly TelemetryClient _telemetryClient;

    public LoadAllSpeakingEngagements(
        ISpeakingEngagementsReader speakerEngagementsReader,
        IEngagementRepository engagementRepository,
        ILogger<LoadAllSpeakingEngagements> logger,
        TelemetryClient telemetryClient)
    {
        _speakerEngagementsReader = speakerEngagementsReader;
        _engagementRepository = engagementRepository;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    [Function(Constants.ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadAll)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req,
        string checkFrom)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadAll, startedAt);

        // Check for the from date
        var dateToCheckFrom = DateTime.MinValue;

        if (!string.IsNullOrEmpty(checkFrom))
        {
            var parsed = DateTime.TryParse(checkFrom, out var dateFrom);
            if (parsed)
            {
                dateToCheckFrom = dateFrom;
            }
        }

        _logger.LogDebug("Getting all items from speaking engagements from '{DateToCheckFrom}'", dateToCheckFrom);
        var newItems = await _speakerEngagementsReader.GetAll(dateToCheckFrom);

        // If there is nothing new, save the last checked value and exit
        if (newItems == null || newItems.Count == 0)
        {
            _logger.LogDebug("No posts found in the Json Feed");
            return new OkObjectResult("0 posts were found");
        }

        // Save the new items to EngagementRepository
        var savedCount = 0;
        foreach (var item in newItems)
        {
            // attempt to save the item
            try
            {
                var engagement = await _engagementRepository.SaveAsync(item);
                var properties = new Dictionary<string, string>
                {
                    { "Id", engagement.Id.ToString() },
                    { "Url", engagement.Url },
                    { "Name", engagement.Name },
                    { "StartDateTime", engagement.StartDateTime.ToString("o") },
                    { "EndDateTime", engagement.EndDateTime.ToString("o") }
                };
                _telemetryClient.TrackEvent(Constants.Metrics.SpeakingEngagementAddedOrUpdated, properties);
                savedCount++;
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to save the engagement with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                    item.Id, item.Url, e);
            }
        }

        // Return
        _logger.LogInformation("Loaded {SavedCount} of {TotalPostsCount} post(s)", savedCount, newItems.Count);
        return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} post(s)");
    }
}