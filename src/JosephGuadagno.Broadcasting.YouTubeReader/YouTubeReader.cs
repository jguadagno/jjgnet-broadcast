using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.YouTubeReader;

public class YouTubeReader: IYouTubeReader
{
    private readonly YouTubeService _youTubeService;
    private readonly IYouTubeSettings _youTubeSettings;
    private readonly ILogger<YouTubeReader> _logger;
        
    public YouTubeReader(IYouTubeSettings youTubeSettings, ILogger<YouTubeReader> logger)
    {
        if (youTubeSettings == null)
        {
            throw new ArgumentNullException(nameof(youTubeSettings), "The YouTube settings are required");
        }
            
        if (string.IsNullOrEmpty(youTubeSettings.ApiKey))
        {
            throw new ArgumentNullException(nameof(youTubeSettings.ApiKey), "The API key of the YouTube settings is required.");
        }

        if (string.IsNullOrEmpty(youTubeSettings.ChannelId))
        {
            throw new ArgumentNullException(nameof(youTubeSettings.ChannelId), "The channel id of the YouTube settings is required.");
        }

        if (string.IsNullOrEmpty(youTubeSettings.PlaylistId))
        {
            throw new ArgumentNullException(nameof(youTubeSettings.PlaylistId), "The playlist id of the YouTube settings is required.");
        }
            
        _youTubeSettings = youTubeSettings;
        if (_youTubeSettings.ResultSetPageSize <= 0 || _youTubeSettings.ResultSetPageSize > 50)
        {
            _youTubeSettings.ResultSetPageSize = 10;
        }
            
        _youTubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = _youTubeSettings.ApiKey,
            ApplicationName = GetType().ToString()
        });

        _logger = logger;
    }

    internal YouTubeReader(IYouTubeSettings youTubeSettings, ILogger<YouTubeReader> logger, YouTubeService youTubeService)
    {
        _youTubeSettings = youTubeSettings;
        _logger = logger;
        _youTubeService = youTubeService;
    }

    public List<YouTubeItem> GetSinceDate(string ownerOid, DateTimeOffset sinceWhen)
    {
        return GetAsync(ownerOid, sinceWhen).Result;
    }

    public Task<List<YouTubeItem>> GetAsync(string ownerOid, DateTimeOffset sinceWhen)
    {
        ValidateOwnerOid(ownerOid);
        return GetItemsAsync(ownerOid, sinceWhen, _youTubeSettings.PlaylistId, _youTubeSettings.ResultSetPageSize, _youTubeService);
    }

    public Task<List<YouTubeItem>> GetAsync(string ownerOid, DateTimeOffset sinceWhen, IYouTubeSettings settings)
    {
        ValidateOwnerOid(ownerOid);
        ArgumentNullException.ThrowIfNull(settings);

        var perUserService = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = settings.ApiKey,
            ApplicationName = GetType().ToString()
        });

        var pageSize = settings.ResultSetPageSize > 0 && settings.ResultSetPageSize <= 50
            ? settings.ResultSetPageSize
            : 10;

        return GetItemsAsync(ownerOid, sinceWhen, settings.PlaylistId, pageSize, perUserService);
    }

    private async Task<List<YouTubeItem>> GetItemsAsync(
        string ownerOid, DateTimeOffset sinceWhen, string playlistId, int pageSize, YouTubeService service)
    {
        var currentTime = DateTime.Now;
        var videoItems = new List<YouTubeItem>();

        var playlistItemsRequest = service.PlaylistItems.List("snippet");
        playlistItemsRequest.PlaylistId = playlistId;
        playlistItemsRequest.MaxResults = pageSize;

        var nextPageToken = "";

        try
        {
            while (nextPageToken != null)
            {
                playlistItemsRequest.PageToken = nextPageToken;
                var playlistItems = await playlistItemsRequest.ExecuteAsync();
                foreach (var playlistItem in playlistItems.Items)
                {
                    if (playlistItem.Kind == "youtube#playlistItem")
                    {
                        if (!playlistItem.Snippet.PublishedAtDateTimeOffset.HasValue)
                        {
                            continue;
                        }

                        var publishedAt = playlistItem.Snippet.PublishedAtDateTimeOffset.Value;

                        if (publishedAt > sinceWhen)
                        {
                            videoItems.Add(new YouTubeItem()
                            {
                                VideoId = playlistItem.Snippet.ResourceId.VideoId,
                                Author = playlistItem.Snippet.ChannelTitle,
                                PublicationDate = publishedAt.UtcDateTime,
                                LastUpdatedOn = publishedAt.UtcDateTime,
                                Title = playlistItem.Snippet.Title,
                                Url = $"https://www.youtube.com/watch?v={playlistItem.Snippet.ResourceId.VideoId}",
                                AddedOn = currentTime,
                                CreatedByEntraOid = ownerOid
                            });
                        }
                        else
                        {
                            // Need to break out of the for loop when the publishDate is less than sinceWhen
                            // This assumes the API returns items from oldest to newest
                            playlistItems.NextPageToken = null;
                            break;
                        }
                    }
                }

                nextPageToken = playlistItems.NextPageToken;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error calling the YouTube API: {PlaylistId}", playlistId);
            throw;
        }

        return videoItems;
    }

    private static void ValidateOwnerOid(string ownerOid)
    {
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            throw new ArgumentException("The owner OID is required.", nameof(ownerOid));
        }
    }
}
