namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

/// <summary>
/// Token information returned by LinkedIn on a refresh request
/// </summary>
public class LinkedInTokenInfo
{
    /// <summary>
    /// The new access token
    /// </summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// Indicates when the access token expires (UTC)
    /// </summary>
    public DateTime ExpiresOn { get; set; }

    /// <summary>
    /// The new refresh token (if one was issued)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Indicates when the refresh token expires (UTC), if provided
    /// </summary>
    public DateTime? RefreshTokenExpiresOn { get; set; }
}
