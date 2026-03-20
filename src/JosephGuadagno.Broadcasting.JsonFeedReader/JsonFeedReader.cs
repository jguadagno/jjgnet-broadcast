using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.JsonFeedReader;

public class JsonFeedReader : IJsonFeedReader
{
    private readonly IJsonFeedReaderSettings _jsonFeedReaderSettings;
    private readonly ILogger<JsonFeedReader> _logger;

    public JsonFeedReader(IJsonFeedReaderSettings jsonFeedReaderSettings, ILogger<JsonFeedReader> logger)
    {
        if (jsonFeedReaderSettings == null)
        {
            throw new ArgumentNullException(nameof(jsonFeedReaderSettings), "The JsonFeedReaderSettings cannot be null");
        }

        if (string.IsNullOrEmpty(jsonFeedReaderSettings.FeedUrl))
        {
            throw new ArgumentNullException(nameof(jsonFeedReaderSettings.FeedUrl), "The FeedUrl of the JsonFeedReaderSettings is required");
        }

        _jsonFeedReaderSettings = jsonFeedReaderSettings;
        _logger = logger;
    }

    public List<JsonFeedSource> GetSinceDate(DateTimeOffset sinceWhen)
    {
        var currentTime = DateTimeOffset.UtcNow;

        _logger.LogDebug("Checking JSON feed '{FeedUrl}' for new posts since '{SinceWhen:u}'",
            _jsonFeedReaderSettings.FeedUrl, sinceWhen);

        List<JsonFeedItem> items;

        try
        {
            using var httpClient = new HttpClient();
            var jsonContent = httpClient.GetStringAsync(_jsonFeedReaderSettings.FeedUrl).Result;
            
            var feed = JsonSerializer.Deserialize<JsonFeedModel>(jsonContent);
            if (feed == null || feed.Items == null)
            {
                _logger.LogWarning("Empty or invalid JSON feed returned from {FeedUrl}", _jsonFeedReaderSettings.FeedUrl);
                return new List<JsonFeedSource>();
            }

            items = feed.Items
                .Where(i => !string.IsNullOrEmpty(i.DatePublished) && 
                           DateTimeOffset.TryParse(i.DatePublished, out var pubDate) && 
                           pubDate > sinceWhen)
                .ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error parsing the JSON feed for: {FeedUrl}",
                _jsonFeedReaderSettings.FeedUrl);
            throw;
        }

        _logger.LogDebug("Found {PostsCount} posts", items.Count);

        return items.Select(item => new JsonFeedSource()
        {
            FeedIdentifier = item.Id ?? item.Url ?? string.Empty,
            Author = item.Authors?.FirstOrDefault()?.Name ?? "Unknown",
            PublicationDate = DateTimeOffset.TryParse(item.DatePublished, out var pubDate) ? pubDate : currentTime,
            ItemLastUpdatedOn = DateTimeOffset.TryParse(item.DateModified, out var modDate) ? modDate : null,
            Title = item.Title ?? string.Empty,
            Url = item.Url ?? string.Empty,
            AddedOn = currentTime,
            LastUpdatedOn = currentTime,
            Tags = item.Tags is null || item.Tags.Length == 0 ? null : string.Join(",", item.Tags)
        })
        .ToList();
    }

    public async Task<List<JsonFeedSource>> GetAsync(DateTimeOffset sinceWhen)
    {
        return await Task.Run(() => GetSinceDate(sinceWhen));
    }

    private class JsonFeedModel
    {
        public string? Version { get; set; }
        public string? Title { get; set; }
        public JsonFeedItem[]? Items { get; set; }
    }

    private class JsonFeedItem
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? DatePublished { get; set; }
        public string? DateModified { get; set; }
        public JsonFeedAuthor[]? Authors { get; set; }
        public string[]? Tags { get; set; }
    }

    private class JsonFeedAuthor
    {
        public string? Name { get; set; }
    }
}
