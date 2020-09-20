using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.FeedReader
{
    public class FeedReader: ISourceReader
    {
        private readonly string _sourceUrl;
        public FeedReader(string sourceUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl))
            {
                throw new ArgumentNullException(nameof(sourceUrl), "The source url is required");
            }

            _sourceUrl = sourceUrl;
        }

        public List<SourceData> Get(DateTime sinceWhen)
        {

            var feedItems = new List<SourceData>();
            if (string.IsNullOrEmpty(_sourceUrl))
            {
                return feedItems;
            }

            using var reader = XmlReader.Create(_sourceUrl);
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
    }
}