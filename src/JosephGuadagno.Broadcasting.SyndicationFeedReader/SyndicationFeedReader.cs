using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader;

public class SyndicationFeedReader: ISyndicationFeedReader
{
    private readonly ISyndicationFeedReaderSettings _syndicationFeedReaderSettings;
    private readonly ILogger<SyndicationFeedReader> _logger;
        
    public SyndicationFeedReader(ISyndicationFeedReaderSettings syndicationFeedReaderSettings, ILogger<SyndicationFeedReader> logger)
    {
        if (syndicationFeedReaderSettings == null)
        {
            throw new ArgumentNullException(nameof(syndicationFeedReaderSettings), "The SyndicationFeedReaderSettings cannot be null");
        }
            
        if (string.IsNullOrEmpty(syndicationFeedReaderSettings.FeedUrl))
        {
            throw new ArgumentNullException(nameof(syndicationFeedReaderSettings.FeedUrl), "The FeedUrl of the SyndicationFeedReaderSettings is required");
        }

        _syndicationFeedReaderSettings = syndicationFeedReaderSettings;
        _logger = logger;
    }

    public List<SyndicationFeedSource> GetSinceDate(DateTimeOffset sinceWhen)
    {
        var currentTime = DateTime.UtcNow;

        _logger.LogDebug("Checking syndication feed '{FeedUrl}' for new posts since '{SinceWhen:u}'",
            _syndicationFeedReaderSettings.FeedUrl, sinceWhen);

        List<SyndicationItem> items;

        try
        {
            using var reader = XmlReader.Create(_syndicationFeedReaderSettings.FeedUrl);
            var feed = SyndicationFeed.Load(reader);

            items = feed.Items.Where(i => (i.PublishDate > sinceWhen) || (i.LastUpdatedTime > sinceWhen))
                .ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error parsing the syndication feed for: {FeedUrl}",
                _syndicationFeedReaderSettings.FeedUrl);
            throw;
        }
            
        _logger.LogDebug("Found {PostsCount} posts", items.Count);

        return items.Select(syndicationItem => new SyndicationFeedSource()
            {
                FeedIdentifier = syndicationItem.Links.FirstOrDefault()?.Uri.AbsoluteUri!,
                Author = syndicationItem.Authors?.FirstOrDefault()?.Name ?? "Unknown",
                PublicationDate = syndicationItem.PublishDate.UtcDateTime,
                ItemLastUpdatedOn = syndicationItem.LastUpdatedTime.UtcDateTime,
                //Text = ((TextSyndicationContent) syndicationItem.Content).Text,
                Title = syndicationItem.Title.Text,
                Url = syndicationItem.Links.FirstOrDefault()?.Uri.AbsoluteUri ?? string.Empty,
                AddedOn = currentTime,
                LastUpdatedOn = currentTime,
                Tags = syndicationItem.Categories is null ? null : string.Join(",", syndicationItem.Categories.Select(c => c.Name))
            })
            .ToList();
    }

    public async Task<List<SyndicationFeedSource>> GetAsync(DateTimeOffset sinceWhen)
    {
        return await Task.Run(() => GetSinceDate(sinceWhen));
    }

    public List<SyndicationFeedSource> GetSyndicationItems(DateTimeOffset sinceWhen, List<string> excludeCategories = null)
    {
        _logger.LogDebug("Checking syndication feed '{FeedUrl}' for posts since '{SinceWhen:u}'",
            _syndicationFeedReaderSettings, sinceWhen);

        DateTimeOffset currentTime = DateTimeOffset.UtcNow;
        List<SyndicationItem> syndicationItems = [];

        try
        {
            using var reader = XmlReader.Create(_syndicationFeedReaderSettings.FeedUrl);
            var feed = SyndicationFeed.Load(reader);

            var recentItems = feed.Items.Where(i => (i.PublishDate > sinceWhen) || (i.LastUpdatedTime > sinceWhen));
            excludeCategories ??= [];
            syndicationItems.AddRange(recentItems
                .Select(item => new
                {
                    item,
                    found = item.Categories.Any(itemCategory =>
                        excludeCategories.Contains(itemCategory.Name.ToLower().Trim()))
                })
                .Where(t => !t.found)
                .Select(t => t.item));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error parsing the syndication feed for: {FeedUrl}",
                _syndicationFeedReaderSettings);
            throw;
        }
            
        _logger.LogDebug("Found {PostsCount} posts", syndicationItems.Count);

        return syndicationItems.Select(syndicationItem => new SyndicationFeedSource()
            {
                FeedIdentifier = syndicationItem.Links.FirstOrDefault()?.Uri.AbsoluteUri!,
                Author = syndicationItem.Authors?.FirstOrDefault()?.Name ?? "Unknown",
                PublicationDate = syndicationItem.PublishDate.UtcDateTime,
                ItemLastUpdatedOn = syndicationItem.LastUpdatedTime.UtcDateTime,
                //Text = ((TextSyndicationContent) syndicationItem.Content).Text,
                Title = syndicationItem.Title.Text,
                Url = syndicationItem.Links.FirstOrDefault()?.Uri.AbsoluteUri ?? string.Empty,
                AddedOn = currentTime,
                LastUpdatedOn = currentTime,
                Tags = syndicationItem.Categories is null ? null : string.Join(",", syndicationItem.Categories.Select(c => c.Name))
            })
            .ToList();
    }

    public SyndicationFeedSource GetRandomSyndicationItem(DateTimeOffset sinceWhen, List<string> excludeCategories = null)
    {
        _logger.LogDebug("Getting random syndication item from feed '{FeedUrl}' since '{SinceWhen:u}'",
            _syndicationFeedReaderSettings.FeedUrl, sinceWhen);

        excludeCategories ??= [];

        var items = GetSyndicationItems(sinceWhen, excludeCategories);

        if (items.Count == 0)
        {
            _logger.LogDebug("No items found to select randomly");
            return null;
        }

        var randomIndex = Random.Shared.Next(items.Count);
        var randomItem = items[randomIndex];

        _logger.LogDebug("Selected random item '{ItemTitle}'", randomItem.Title);

        return randomItem;
    }
}