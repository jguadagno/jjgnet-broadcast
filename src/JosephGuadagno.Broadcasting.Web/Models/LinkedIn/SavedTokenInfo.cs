namespace JosephGuadagno.Broadcasting.Web.Models.LinkedIn;

public class SavedTokenInfo
{
    /// <summary>
    /// Masked representation of the access token (never the raw value).
    /// </summary>
    public string? MaskedAccessToken { get; set; }
    public bool HasToken { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public int? PlatformId { get; set; }
}