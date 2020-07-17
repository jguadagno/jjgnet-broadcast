namespace JosephGuadagno.Broadcasting.Domain.Interfaces
{
    public interface ISettings
    {
        public string StorageAccount { get; }
        public string TwitterApiKey { get; }
        public string TwitterApiSecret { get; }
        public string TwitterAccessToken { get; }
        public string TwitterAccessTokenSecret { get; }
        public string FeedUrl { get; }
        public string BitlyToken { get; }
        public string BitlyAPIRootUri { get; }
        
        public string TopicNewSourceDataEndpoint { get; }
        public string TopicNewSourceDataKey { get; }
    }
}