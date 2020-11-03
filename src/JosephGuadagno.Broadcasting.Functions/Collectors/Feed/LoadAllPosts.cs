using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.Feed
{
    public class LoadAllPosts
    {
        private readonly IJsonFeedReader _jsonFeedReader;
        private readonly ISettings _settings;
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly IUrlShortener _urlShortener;
        private readonly ILogger<LoadAllPosts> _logger;

        public LoadAllPosts(IJsonFeedReader jsonFeedReader, ISettings settings, 
            SourceDataRepository sourceDataRepository,
            IUrlShortener urlShortener,
            ILogger<LoadAllPosts> logger)
        {
            _settings = settings;
            _sourceDataRepository = sourceDataRepository;
            _urlShortener = urlShortener;
            _jsonFeedReader = jsonFeedReader;
            _logger = logger;
        }

        [FunctionName("collectors_feed_load_all_posts")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            Domain.Models.LoadJsonFeedItemsRequest requestModel,
            HttpRequest req)
        {
            var startedAt = DateTime.UtcNow;
            _logger.LogDebug($"{Constants.ConfigurationFunctionNames.CollectorsFeedLoadAllPosts} Collector started at: {startedAt}");

            // Check for the from date
            var dateToCheckFrom = DateTime.MinValue;
            if (requestModel != null)
            {
                dateToCheckFrom = requestModel.CheckFrom;
            }

            _logger.LogDebug($"Getting all items from feed from '{dateToCheckFrom}'.");
            var newItems = await _jsonFeedReader.GetAsync(dateToCheckFrom);
            
            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                _logger.LogDebug($"No posts found in the Json Feed.");
                return new OkObjectResult("0 posts were found");
            }
            
            // Save the new items to SourceDataRepository
            // TODO: Handle duplicate posts?
            // GitHub Issue #4
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
                    _logger.LogError($"Was not able to save post with the id of '{item.Id}'. Exception: {e.Message}");
                }
                
                if (!saveWasSuccessful)
                {
                    _logger.LogError($"Was not able to save post with the id of '{item.Id}'.");
                }
                else
                {
                    savedCount++;
                }
            }
            
            // Return
            var doneMessage = $"Loaded {savedCount} of {newItems.Count} post(s).";
            _logger.LogDebug(doneMessage);
            return new OkObjectResult(doneMessage);
        }
    }
}