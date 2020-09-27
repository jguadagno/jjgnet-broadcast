namespace JosephGuadagno.Broadcasting.Domain.Interfaces
{
    public interface ISettings
    {
        public string StorageAccount { get; set; }
        public string TwitterApiKey { get; set; }
        public string TwitterApiSecret { get; set; }
        public string TwitterAccessToken { get; set; }
        public string TwitterAccessTokenSecret { get; set; }
        public string FeedUrl { get; set; }
        public string JsonFeedUrl { get; set; }
        public string BitlyToken { get; set; }
        public string BitlyAPIRootUri { get; set; }
        
        public string TopicNewSourceDataEndpoint { get; set; }
        public string TopicNewSourceDataKey { get; set; }
        
        public string FacebookPageId { get; set; }
        public string FacebookPageAccessToken { get; set; }
        
        public string YouTubeApiKey { get; set; }
        public string YouTubeChannelId { get; set; }
        public string YouTubePlaylistId { get; set; }
        public int YouTubePagedVideoResults { get; set; }
    }
}