using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader
{
    public class SyndicationFeedReader: ISyndicationFeedReader
    {
        private readonly ISyndicationFeedReaderSettings _syndicationFeedReaderSettings;
        
        public SyndicationFeedReader(ISyndicationFeedReaderSettings syndicationFeedReaderSettings)
        {
            if (syndicationFeedReaderSettings == null)
            {
                throw new ArgumentNullException(nameof(syndicationFeedReaderSettings), "The SyndicationFeedReaderSettings cannot be null");
            }
            
            if (string.IsNullOrEmpty(syndicationFeedReaderSettings.FeedUrl))
            {
                throw new ArgumentNullException(nameof(syndicationFeedReaderSettings.FeedUrl), "The FeedUrl of the SyndicationFeedReaderSettings" +
                    " is required");
            }

            _syndicationFeedReaderSettings = syndicationFeedReaderSettings;
        }

        public List<SourceData> Get(DateTime sinceWhen)
        {
            var feedItems = new List<SourceData>();

            using var reader = XmlReader.Create(_syndicationFeedReaderSettings.FeedUrl);
            var feed = SyndicationFeed.Load(reader);

            var items = feed.Items.Where(i => i.PublishDate >= sinceWhen).ToList();

            foreach (var syndicationItem in items)
            {
                feedItems.Add(new SourceData(SourceSystems.SyndicationFeed, syndicationItem.Id)
                {
                    Author = syndicationItem.Authors.FirstOrDefault()?.Name,
                    PublicationDate = syndicationItem.PublishDate,
                    //Text = ((TextSyndicationContent) syndicationItem.Content).Text,
                    Title =  syndicationItem.Title.Text,
                    Url = syndicationItem.Id,
                    EndAfter = null,
                    AddedOn = DateTimeOffset.UtcNow
                });
            }
            return feedItems;
        }

        public async Task<List<SourceData>> GetAsync(DateTime sinceWhen)
        {
            return Get(sinceWhen);
        }
    }
}