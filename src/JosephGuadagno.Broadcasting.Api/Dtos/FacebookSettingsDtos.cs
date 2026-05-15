namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>Request DTO for creating or updating Facebook publisher settings.</summary>
public class FacebookSettingsRequest
{
    public bool IsEnabled { get; set; }
    public string? PageId { get; set; }
    public string? AppId { get; set; }
    public string? PageAccessToken { get; set; }
    public string? AppSecret { get; set; }
    public string? ClientToken { get; set; }
    public string? ShortLivedAccessToken { get; set; }
    public string? LongLivedAccessToken { get; set; }
}

/// <summary>Response DTO for Facebook publisher settings.</summary>
public class FacebookSettingsResponse
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? PageId { get; set; }
    public string? AppId { get; set; }
    public bool HasPageAccessToken { get; set; }
    public bool HasAppSecret { get; set; }
    public bool HasClientToken { get; set; }
    public bool HasShortLivedAccessToken { get; set; }
    public bool HasLongLivedAccessToken { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}
