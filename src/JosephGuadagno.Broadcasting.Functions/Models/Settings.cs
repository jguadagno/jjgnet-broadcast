using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;

namespace JosephGuadagno.Broadcasting.Functions.Models;

public class Settings : ISettings
{
    public string StorageAccount { get; set; }
    public string TwitterApiKey { get; set; }
    public string TwitterApiSecret { get; set; }
    public string TwitterAccessToken { get; set; }
    public string TwitterAccessTokenSecret { get; set; }
    public string BitlyToken { get; set; }
    public string BitlyAPIRootUri { get; set; }
    public string BitlyShortenedDomain { get; set; }

    /// <summary>
    /// The AutoMapper settings.
    /// </summary>
    public required AutoMapperSettings AutoMapper { get; init; }
}