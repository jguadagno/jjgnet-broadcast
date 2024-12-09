using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using JsonFeedNet;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.JsonFeedReader;

public class JsonFeedReader: IJsonFeedReader
{
    private readonly IJsonFeedReaderSettings _jsonFeedReaderSettings;
    private readonly ILogger<JsonFeedReader> _logger;
        
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
        
    public List<SourceData> GetSinceDate(DateTime sinceWhen)
    {
        return GetAsync(sinceWhen).Result;
    }

    public async Task<List<SourceData>> GetAsync(DateTime sinceWhen)
    {
        var currentTime = DateTime.UtcNow;
        var sourceItems = new List<SourceData>();

        _logger.LogDebug("Checking the Json feed '{FeedUrl}' for new posts since '{SinceWhen:u}'",
            _jsonFeedReaderSettings.FeedUrl, sinceWhen);

        List<JsonFeedItem> items;
        try
        {
            var jsonFeed = await JsonFeed.ParseFromUriAsync(new Uri(_jsonFeedReaderSettings.FeedUrl));

            items = jsonFeed.Items.Where(i => i.DateModified > sinceWhen || i.DatePublished > sinceWhen).ToList();

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error parsing the Json feed for: {FeedUrl}",
                _jsonFeedReaderSettings.FeedUrl);
            throw;
        }
        _logger.LogDebug("Found {PostCount} posts", items.Count);
            
        foreach (var jsonFeedItem in items)
        {
            sourceItems.Add(new SourceData(SourceSystems.SyndicationFeed)
            {
                Author = jsonFeedItem.Authors?[0]?.Name,
                PublicationDate = jsonFeedItem.DatePublished?.UtcDateTime ?? currentTime,
                UpdatedOnDate = jsonFeedItem.DateModified?.UtcDateTime ?? currentTime,
                Title =  jsonFeedItem.Title,
                Url = jsonFeedItem.Id,
                EndAfter = null,
                AddedOn = currentTime,
                Tags = jsonFeedItem.Tags is null ? null : string.Join(",", jsonFeedItem.Tags)
            });
        }
        return sourceItems;
    }
}