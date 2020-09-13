using System;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    public class Settings : ISettings
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
    }
}