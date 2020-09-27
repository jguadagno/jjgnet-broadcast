using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader;
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

        public LoadAllVideos(IYouTubeReader youTubeReader, ISettings settings, 
            SourceDataRepository sourceDataRepository,
            IUrlShortener urlShortener)
        {
            _settings = settings;
            _sourceDataRepository = sourceDataRepository;
            _urlShortener = urlShortener;
            _youTubeReader = youTubeReader;
        }

        [FunctionName("collectors_youtube_load_all_videos")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            Domain.Models.LoadJsonFeedItemsRequest requestModel,
            HttpRequest req,
            ILogger log)
        {
            var startedAt = DateTime.UtcNow;
            log.LogInformation($"{Constants.ConfigurationFunctionNames.CollectorsYouTubeLoadAllVideos} Collector started at: {startedAt}");

            // Check for the from date
            var dateToCheckFrom = DateTime.MinValue;
            if (requestModel != null)
            {
                dateToCheckFrom = requestModel.CheckFrom;
            }

            log.LogInformation($"Getting all items from YouTube '{_settings.YouTubeChannelId}'.");
            var newItems = await _youTubeReader.GetAsync(dateToCheckFrom);
            
            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                log.LogDebug($"No videos found at '{_settings.YouTubeChannelId}'.");
                return new OkObjectResult("0 videos were found");
            }
            
            // Save the new items to SourceDataRepository
            // TODO: Handle duplicate posts?
            // GitHub Issue #17
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
                    log.LogError($"Was not able to save video with the id of '{item.Id}'. Exception: {e.Message}");
                }
                
                if (!saveWasSuccessful)
                {
                    log.LogError($"Was not able to save video with the id of '{item.Id}'.");
                }
                else
                {
                    savedCount++;
                }
            }
            
            // Return
            var doneMessage = $"Loaded {savedCount} of {newItems.Count} videos(s).";
            log.LogInformation(doneMessage);
            return new OkObjectResult(doneMessage);
        }
    }
}