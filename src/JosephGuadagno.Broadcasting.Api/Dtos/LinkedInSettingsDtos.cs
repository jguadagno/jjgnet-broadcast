namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>Request DTO for creating or updating LinkedIn publisher settings.</summary>
public class LinkedInSettingsRequest
{
    public bool IsEnabled { get; set; }
    public string? AuthorId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AccessToken { get; set; }
}

/// <summary>Response DTO for LinkedIn publisher settings.</summary>
public class LinkedInSettingsResponse
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? AuthorId { get; set; }
    public string? ClientId { get; set; }
    public bool HasClientSecret { get; set; }
    public bool HasAccessToken { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}
