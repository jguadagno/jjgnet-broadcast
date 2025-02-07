using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Extensions.Types;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;

public class LoadAllVideos
{
    private readonly IYouTubeReader _youTubeReader;
    private readonly ISettings _settings;
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly IUrlShortener _urlShortener;
    private readonly ILogger<LoadAllVideos> _logger;
    private readonly TelemetryClient _telemetryClient;

    public LoadAllVideos(IYouTubeReader youTubeReader,
        ISettings settings, 
        SourceDataRepository sourceDataRepository,
        IUrlShortener urlShortener,
        ILogger<LoadAllVideos> logger,
        TelemetryClient telemetryClient)
    {
        _settings = settings;
        _sourceDataRepository = sourceDataRepository;
        _urlShortener = urlShortener;
        _youTubeReader = youTubeReader;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    [Function(Constants.ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req, 
        string checkFrom)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos, startedAt);

        // Check for the from date
        var dateToCheckFrom = DateTime.MinValue;

        if (!checkFrom.IsNullOrEmpty())
        {
            var parsed = DateTime.TryParse(checkFrom, out var dateFrom);
            if (parsed)
            {
                dateToCheckFrom = dateFrom;
            }
        }

        _logger.LogDebug("Getting all items from YouTube for the playlist since '{DateToCheckFrom}'", dateToCheckFrom);
        var newItems = await _youTubeReader.GetAsync(dateToCheckFrom);
            
        // If there is nothing new, save the last checked value and exit
        if (newItems == null || newItems.Count == 0)
        {
            _logger.LogInformation("No videos found in the playlist");
            return new OkObjectResult("0 videos were found");
        }
            
        // Save the new items to SourceDataRepository
        var savedCount = 0;
        foreach (var item in newItems)
        {
            // shorten the url
            item.ShortenedUrl = await _urlShortener.GetShortenedUrlAsync(item.Url, _settings.BitlyShortenedDomain);
                
            // attempt to save the item
            try
            {
                var saveWasSuccessful = await _sourceDataRepository.SaveAsync(item);
                if (saveWasSuccessful)
                {
                    _telemetryClient.TrackEvent(Constants.Metrics.VideoAddedOrUpdated, item.ToDictionary());
                    savedCount++;
                }
                else
                {
                    _logger.LogError("Failed to save the video with the id of: '{Id}' Url:'{Url}'", item.Id, item.Url);
                }
                    
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to save the video with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                    item.Id, item.Url, e);
            }
        }
            
        // Return
        _logger.LogInformation("Loaded {SavedCount} of {TotalVideoCount} videos(s)", savedCount, newItems.Count);
        return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} videos(s)");
    }
}