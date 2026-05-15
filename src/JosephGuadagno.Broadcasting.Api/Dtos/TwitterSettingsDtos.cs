namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>Request DTO for creating or updating Twitter publisher settings.</summary>
public class TwitterSettingsRequest
{
    public bool IsEnabled { get; set; }
    public string? ConsumerKey { get; set; }
    public string? ConsumerSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? AccessTokenSecret { get; set; }
}

/// <summary>Response DTO for Twitter publisher settings.</summary>
public class TwitterSettingsResponse
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool HasConsumerKey { get; set; }
    public bool HasConsumerSecret { get; set; }
    public bool HasAccessToken { get; set; }
    public bool HasAccessTokenSecret { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}
