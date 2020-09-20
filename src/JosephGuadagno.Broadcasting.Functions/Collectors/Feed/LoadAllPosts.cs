using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Utilities.Web.Shortener;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.Feed
{
    public class LoadAllPosts
    {
        private readonly ISettings _settings;
        private readonly SourceDataRepository _sourceDataRepository;
        private readonly Bitly _bitly;

        public LoadAllPosts(ISettings settings, 
            SourceDataRepository sourceDataRepository,
            Bitly bitly)
        {
            _settings = settings;
            _sourceDataRepository = sourceDataRepository;
            _bitly = bitly;
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
            var feedReader = new JsonFeedReader.JsonFeedReader(_settings.JsonFeedUrl);
            var newItems = feedReader.Get(dateToCheckFrom);
            
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
                item.ShortenedUrl = await GetShortenedUrlAsync(item.Url);
                
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
        
        private async Task<string> GetShortenedUrlAsync(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl))
            {
                return null;
            }

            var result = await _bitly.Shorten(originalUrl, "jjg.me");
            return result == null ? originalUrl : result.Link;
        }
    }
}