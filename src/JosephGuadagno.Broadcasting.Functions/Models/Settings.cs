using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;

namespace JosephGuadagno.Broadcasting.Functions.Models;

public class Settings : ISettings, IDatabaseSettings
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
    public string JJGNetDatabaseSqlServer { get; set; }
    public string TopicNewRandomPostEndpoint { get; set; }
    public string TopicNewRandomPostKey { get; set; }
    public string BlueskyUserName { get; set; }
    public string BlueskyPassword { get; set; }
    
    /// <summary>
    /// The Azure Key Vault to use
    /// </summary>
    public KeyVaultSettings KeyVault { get; set; }

    /// <summary>
    /// The AutoMapper settings.
    /// </summary>
    public required AutoMapperSettings AutoMapper { get; init; }
}