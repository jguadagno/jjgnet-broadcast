using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.YouTubeReader
{
    public class YouTubeReader: IYouTubeReader
    {
        private readonly string _apiKey;
        private readonly string _channelId;
        private readonly YouTubeService _youTubeService;
        private readonly string _playlistId;

        public YouTubeReader(string apiKey, string channelId, string playlistId)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "The api key is required.");
            }

            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId), "The channel id is required.");
            }

            if (string.IsNullOrEmpty(playlistId))
            {
                throw new ArgumentNullException(nameof(playlistId), "The playlist id is required.");
            }

            _apiKey = apiKey;
            _channelId = channelId;
            _playlistId = playlistId;
            
            _youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = GetType().ToString()
            });
        }

        public List<SourceData> Get(DateTime sinceWhen)
        {
            return GetAsync(sinceWhen).Result;
        }

        public async Task<List<SourceData>> GetAsync(DateTime sinceWhen)
        {
            var videoItems = new List<SourceData>();
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_channelId))
            {
                return videoItems;
            }
            
            var playlistItemsRequest = _youTubeService.PlaylistItems.List("snippet");
            playlistItemsRequest.PlaylistId = _playlistId;
            playlistItemsRequest.MaxResults = 10; // TODO: Make this a configurable value
            
            var nextPageToken = "";
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
                       
                        if (publishedAt >= sinceWhen)
                        {
                            videoItems.Add(new SourceData(SourceSystems.YouTube,
                                playlistItem.Snippet.ResourceId.VideoId)
                            {
                                // TODO: Look to get author name and not channel name
                                Author = playlistItem.Snippet.ChannelTitle,
                                // TODO: Safely parse the date time.
                                PublicationDate = publishedAt,
                                //Text = searchResult.Snippet.Description,
                                Title = playlistItem.Snippet.Title,
                                Url = $"https://www.youtube.com/watch?v={playlistItem.Snippet.ResourceId.VideoId}",
                                EndAfter = null,
                                AddedOn = DateTimeOffset.UtcNow
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

            return videoItems;
        }
    }
}