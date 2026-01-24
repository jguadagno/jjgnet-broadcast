using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using JosephGuadagno.Broadcasting.Domain;
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

    public List<SourceData> GetSinceDate(DateTime sinceWhen)
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

        return items.Select(syndicationItem => new SourceData(SourceSystems.SyndicationFeed, syndicationItem.Links.FirstOrDefault()?.Uri.AbsoluteUri)
            {
                Author = syndicationItem.Authors.FirstOrDefault()?.Name,
                PublicationDate = syndicationItem.PublishDate.UtcDateTime,
                UpdatedOnDate = syndicationItem.LastUpdatedTime.UtcDateTime,
                //Text = ((TextSyndicationContent) syndicationItem.Content).Text,
                Title = syndicationItem.Title.Text,
                Url = syndicationItem.Links.FirstOrDefault()?.Uri.AbsoluteUri,
                EndAfter = null,
                AddedOn = currentTime,
                Tags = syndicationItem.Categories is null ? null : string.Join(",", syndicationItem.Categories.Select(c => c.Name))
            })
            .ToList();
    }

    public async Task<List<SourceData>> GetAsync(DateTime sinceWhen)
    {
        return await Task.Run(() => GetSinceDate(sinceWhen));
    }

    public List<SyndicationItem> GetSyndicationItems(DateTime sinceWhen, List<string> excludeCategories)
    {
        _logger.LogDebug("Checking syndication feed '{FeedUrl}' for posts since '{SinceWhen:u}'",
            _syndicationFeedReaderSettings, sinceWhen);

        List<SyndicationItem> items = [];

        try
        {
            using var reader = XmlReader.Create(_syndicationFeedReaderSettings.FeedUrl);
            var feed = SyndicationFeed.Load(reader);

            var recentItems = feed.Items.Where(i => (i.PublishDate > sinceWhen) || (i.LastUpdatedTime > sinceWhen));
            items.AddRange(from item in recentItems
                let found = item.Categories.Any(itemCategory =>
                    excludeCategories.Contains(itemCategory.Name.ToLower().Trim()))
                where !found
                select item);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error parsing the syndication feed for: {FeedUrl}",
                _syndicationFeedReaderSettings);
            throw;
        }
            
        _logger.LogDebug("Found {PostsCount} posts", items.Count);

        return items;
    }

    public SyndicationItem GetRandomSyndicationItem(DateTime sinceWhen, List<string> excludeCategories = null)
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

        _logger.LogDebug("Selected random item '{ItemTitle}'", randomItem.Title?.Text ?? "Untitled");

        return randomItem;
    }
}