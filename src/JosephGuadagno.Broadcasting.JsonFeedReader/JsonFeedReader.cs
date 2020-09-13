using System;
using System.Collections.Generic;
using System.Linq;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JsonFeedNet;

namespace JosephGuadagno.Broadcasting.JsonFeedReader
{
    public class JsonFeedReader: ISourceReader
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
            var sourceItems = new List<SourceData>();
            if (string.IsNullOrEmpty(_sourceUrl))
            {
                return sourceItems;
            }

            // TODO: Make this call and the Get method Async
            var jsonFeed = JsonFeed.ParseFromUriAsync(new Uri(_sourceUrl)).Result;

            var items = jsonFeed.Items.Where(i => i.DatePublished >= sinceWhen).ToList();

            foreach (var jsonFeedItem in items)
            {
                sourceItems.Add(new SourceData("RssFeed")
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