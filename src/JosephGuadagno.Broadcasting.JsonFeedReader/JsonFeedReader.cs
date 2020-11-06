using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using JsonFeedNet;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.JsonFeedReader
{
    public class JsonFeedReader: IJsonFeedReader
    {
        private readonly IJsonFeedReaderSettings _jsonFeedReaderSettings;
        private readonly ILogger _logger;
        
        public JsonFeedReader(IJsonFeedReaderSettings jsonFeedReaderSettings, ILogger<JsonFeedReader> logger)
        {
            if (jsonFeedReaderSettings == null)
            {
                throw new ArgumentNullException(nameof(jsonFeedReaderSettings), "The JsonFeedReaderSettings can not be null");
            }
            
            if (string.IsNullOrEmpty(jsonFeedReaderSettings.FeedUrl))
            {
                throw new ArgumentNullException(nameof(jsonFeedReaderSettings.FeedUrl), "The FeedUrl of the JsonFeedReaderSettings is required");
            }

            _jsonFeedReaderSettings = jsonFeedReaderSettings;
            _logger = logger;

        }
        
        public List<SourceData> Get(DateTime sinceWhen)
        {
            return GetAsync(sinceWhen).Result;
        }

        public async Task<List<SourceData>> GetAsync(DateTime sinceWhen)
        {
            var sourceItems = new List<SourceData>();

            var jsonFeed = await JsonFeed.ParseFromUriAsync(new Uri(_jsonFeedReaderSettings.FeedUrl));

            var items = jsonFeed.Items.Where(i => i.DateModified >= sinceWhen).ToList();

            foreach (var jsonFeedItem in items)
            {
                sourceItems.Add(new SourceData(SourceSystems.SyndicationFeed)
                {
                    Author = jsonFeedItem.Author?.Name,
                    PublicationDate = jsonFeedItem.DatePublished ?? DateTimeOffset.UtcNow,
                    //Text = jsonFeedItem.ContentHtml,
                    Title =  jsonFeedItem.Title,
                    Url = jsonFeedItem.Id,
                    EndAfter = null,
                    AddedOn = DateTimeOffset.UtcNow
                });
            }
            return sourceItems;
        }
    }
}