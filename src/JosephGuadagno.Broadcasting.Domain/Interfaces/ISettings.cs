namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ISettings
{
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
    public string TopicNewRandomPostEndpoint { get; set; }
    public string TopicNewRandomPostKey { get; set; }

    public string BlueskyUserName { get; set; }
    public string BlueskyPassword { get; set; }
    
    public string AzureKeyVaultUrl { get; set; }
}