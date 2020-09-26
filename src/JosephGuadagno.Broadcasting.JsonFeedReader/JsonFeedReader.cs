using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JsonFeedNet;

namespace JosephGuadagno.Broadcasting.JsonFeedReader
{
    public class JsonFeedReader: IJsonReader
    {
        private readonly string _sourceUrl;
        
        public JsonFeedReader(string sourceUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl))
            {
                throw new ArgumentNullException(nameof(sourceUrl), "The source url is required");
            }
            
            _sourceUrl = sourceUrl;
        }
        
        public List<SourceData> Get(DateTime sinceWhen)
        {
            return GetAsync(sinceWhen).Result;
        }

        public async Task<List<SourceData>> GetAsync(DateTime sinceWhen)
        {
            var sourceItems = new List<SourceData>();
            if (string.IsNullOrEmpty(_sourceUrl))
            {
                return sourceItems;
            }

            var jsonFeed = await JsonFeed.ParseFromUriAsync(new Uri(_sourceUrl));

            var items = jsonFeed.Items.Where(i => i.DatePublished >= sinceWhen).ToList();

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