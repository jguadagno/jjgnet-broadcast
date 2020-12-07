using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.YouTubeReader
{
    public class YouTubeReader: IYouTubeReader
    {
        private readonly YouTubeService _youTubeService;
        private readonly IYouTubeSettings _youTubeSettings;
        private readonly ILogger _logger;
        
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

        public List<SourceData> Get(DateTime sinceWhen)
        {
            return GetAsync(sinceWhen).Result;
        }

        public async Task<List<SourceData>> GetAsync(DateTime sinceWhen)
        {
            var currentTime = DateTime.Now;
            var videoItems = new List<SourceData>();
            
            var playlistItemsRequest = _youTubeService.PlaylistItems.List("snippet");
            playlistItemsRequest.PlaylistId = _youTubeSettings.PlaylistId;
            playlistItemsRequest.MaxResults = _youTubeSettings.ResultSetPageSize;
            
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
                            if (!DateTime.TryParse(playlistItem.Snippet.PublishedAt, out var publishedAt))
                            {
                                continue;
                            }
                       
                            if (publishedAt > sinceWhen)
                            {
                                videoItems.Add(new SourceData(SourceSystems.YouTube,
                                    playlistItem.Snippet.ResourceId.VideoId)
                                {
                                    Author = playlistItem.Snippet.ChannelTitle,
                                    PublicationDate = publishedAt,
                                    UpdatedOnDate = publishedAt,
                                    //Text = searchResult.Snippet.Description,
                                    Title = playlistItem.Snippet.Title,
                                    Url = $"https://www.youtube.com/watch?v={playlistItem.Snippet.ResourceId.VideoId}",
                                    EndAfter = null,
                                    AddedOn = currentTime
                                });
                            }
                            else
                            {
                                // Need to break out of the for when the publishDate is less than sinceWhen
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
                _logger.LogError(e, "Error calling the YouTube API: {_youTubeSettings.ChannelId}, {_youTubeSettings.PlaylistId}.",
                    _youTubeSettings);
                throw;
            }

            return videoItems;
        }
    }
}