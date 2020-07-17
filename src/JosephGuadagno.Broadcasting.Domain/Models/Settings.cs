using System;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    public class Settings : ISettings
    {
        public string StorageAccount => Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        public string TwitterApiKey => Environment.GetEnvironmentVariable("Twitter-Api-Key");
        public string TwitterApiSecret => Environment.GetEnvironmentVariable("Twitter-Api-Secret");
        public string TwitterAccessToken => Environment.GetEnvironmentVariable("Twitter-Access-Token");
        public string TwitterAccessTokenSecret => Environment.GetEnvironmentVariable("Twitter-Access-Token-Secret");
        public string FeedUrl => Environment.GetEnvironmentVariable("Feed-Url");
        public string BitlyToken => Environment.GetEnvironmentVariable("Bitly-Token");
        public string BitlyAPIRootUri => Environment.GetEnvironmentVariable("Bitly-APIRootUri");
        public string TopicNewSourceDataEndpoint => Environment.GetEnvironmentVariable("Topic-New-Source-Data-Endpoint");
        public string TopicNewSourceDataKey => Environment.GetEnvironmentVariable("Topic-New-Source-Data-Key");
    }
}