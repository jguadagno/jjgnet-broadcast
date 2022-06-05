using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class Settings : ISettings, IDatabaseSettings
{
    public string AppInsightsKey { get; set; }
    public string StorageAccount { get; set; }
    public string TwitterApiKey { get; set; }
    public string TwitterApiSecret { get; set; }
    public string TwitterAccessToken { get; set; }
    public string TwitterAccessTokenSecret { get; set; }
    public string BitlyToken { get; set; }
    public string BitlyAPIRootUri { get; set; }
    public string BitlyShortenedDomain { get; set; }
    public string TopicNewSourceDataEndpoint { get; set; }
    public string TopicNewSourceDataKey { get; set; }
    public string TopicScheduledItemFiredDataEndpoint { get; set; }
    public string TopicScheduledItemFiredDataKey { get; set; }
    public string FacebookPageId { get; set; }
    public string FacebookPageAccessToken { get; set; }
    public string JJGNetDatabaseSqlServer { get; set; }
}