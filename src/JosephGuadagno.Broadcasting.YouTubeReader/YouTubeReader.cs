using System;
using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Azure.Documents.SystemFunctions;

namespace JosephGuadagno.Broadcasting.YouTubeReader
{
    public class YouTubeReader: IYouTubeReader
    {
        private readonly string _apiKey;
        private readonly string _channelId;
        private readonly YouTubeService _youTubeService;
        
        public YouTubeReader(string apiKey, string channelId)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "The api key is required.");
            }

            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId), "The channel id is required.");
            }

            _apiKey = apiKey;
            _channelId = channelId;
            
            _youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = this.GetType().ToString()
            });
        }

        // TODO: Convert to Async
        public List<SourceData> Get(DateTime sinceWhen)
        {
            var videoItems = new List<SourceData>();
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_channelId))
            {
                return videoItems;
            }

            var searchRequest = _youTubeService.Search.List("snippet");
            searchRequest.ChannelId = _channelId;
            searchRequest.PublishedAfter = sinceWhen;
            searchRequest.Type = "video";
            var searchResponse = searchRequest.ExecuteAsync().Result;

            foreach (var searchResult in searchResponse.Items)
            {
                if (searchResult.Kind == "youtube#searchResult")
                {
                    videoItems.Add(new SourceData(SourceSystems.YouTube, searchResult.Id.VideoId)
                    {
                        // TODO: Look to get author name and not channel name
                        Author = searchResult.Snippet.ChannelTitle,
                        // TODO: Safely parse the date time.
                        PublicationDate = DateTimeOffset.Parse(searchResult.Snippet.PublishedAt),
                        //Text = searchResult.Snippet.Description,
                        Title =  searchResult.Snippet.Title,
                        Url = $"https://www.youtube.com/watch?v={searchResult.Id.VideoId}",
                        EndAfter = null,
                        AddedOn = DateTimeOffset.UtcNow
                    });
                }
            }
            
            return videoItems;
        }
    }
}