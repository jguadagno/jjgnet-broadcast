using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.YouTube
{
    public class LoadAllVideos
    {
        private readonly IYouTubeReader _youTubeReader;
        private readonly ISettings _settings;
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly IUrlShortener _urlShortener;
        private readonly ILogger<LoadAllVideos> _logger;

        public LoadAllVideos(IYouTubeReader youTubeReader, ISettings settings, 
            SourceDataRepository sourceDataRepository,
            IUrlShortener urlShortener,
            ILogger<LoadAllVideos> logger)
        {
            _settings = settings;
            _sourceDataRepository = sourceDataRepository;
            _urlShortener = urlShortener;
            _youTubeReader = youTubeReader;
            _logger = logger;
        }

        [FunctionName("collectors_youtube_load_all_videos")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            Domain.Models.LoadJsonFeedItemsRequest requestModel,
            HttpRequest req)
        {
            var startedAt = DateTime.UtcNow;
            _logger.LogDebug($"{Constants.ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos} Collector started at: {startedAt}");

            // Check for the from date
            var dateToCheckFrom = DateTime.MinValue;
            if (requestModel != null)
            {
                dateToCheckFrom = requestModel.CheckFrom;
            }

            _logger.LogDebug($"Getting all items from YouTube for the playlist'.");
            var newItems = await _youTubeReader.GetAsync(dateToCheckFrom);
            
            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                _logger.LogDebug($"No videos found in the playlist.");
                return new OkObjectResult("0 videos were found");
            }
            
            // Save the new items to SourceDataRepository
            // TODO: Handle duplicate posts?
            // GitHub Issue #17
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // shorten the url
                item.ShortenedUrl = await _urlShortener.GetShortenedUrlAsync(item.Url, _settings.BitlyShortenedDomain);
                
                // attempt to save the item
                var saveWasSuccessful = false;
                try
                {
                    saveWasSuccessful = await _sourceDataRepository.SaveAsync(item);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Was not able to save video with the id of '{item.Id}'. Exception: {e.Message}");
                }
                
                if (!saveWasSuccessful)
                {
                    _logger.LogError($"Was not able to save video with the id of '{item.Id}'.");
                }
                else
                {
                    savedCount++;
                }
            }
            
            // Return
            var doneMessage = $"Loaded {savedCount} of {newItems.Count} videos(s).";
            _logger.LogDebug(doneMessage);
            return new OkObjectResult(doneMessage);
        }
    }
}