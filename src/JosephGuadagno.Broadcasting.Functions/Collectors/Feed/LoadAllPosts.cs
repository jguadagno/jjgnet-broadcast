using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.Feed
{
    public class LoadAllPosts
    {
        private readonly IJsonReader _jsonReader;
        private readonly ISettings _settings;
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly IUrlShortener _urlShortener;

        public LoadAllPosts(IJsonReader jsonReader, ISettings settings, 
            SourceDataRepository sourceDataRepository,
            IUrlShortener urlShortener)
        {
            _settings = settings;
            _sourceDataRepository = sourceDataRepository;
            _urlShortener = urlShortener;
            _jsonReader = jsonReader;
        }

        [FunctionName("collectors_feed_load_all_posts")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            Domain.Models.LoadJsonFeedItemsRequest requestModel,
            HttpRequest req,
            ILogger log)
        {
            var startedAt = DateTime.UtcNow;
            log.LogInformation($"{Constants.ConfigurationFunctionNames.CollectorsFeedLoadAllPosts} Collector started at: {startedAt}");

            // Check for the from date
            var dateToCheckFrom = DateTime.MinValue;
            if (requestModel != null)
            {
                dateToCheckFrom = requestModel.CheckFrom;
            }

            log.LogInformation($"Getting all items from feed '{_settings.JsonFeedUrl}'.");
            var newItems = _jsonReader.Get(dateToCheckFrom);
            
            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                log.LogDebug($"No posts found at '{_settings.JsonFeedUrl}'.");
                return new OkObjectResult("0 posts were found");
            }
            
            // Save the new items to SourceDataRepository
            // TODO: Handle duplicate posts?
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
            
            // Return
            var doneMessage = $"Loaded {savedCount} of {newItems.Count} post(s).";
            log.LogInformation(doneMessage);
            return new OkObjectResult(doneMessage);
        }
    }
}