namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API request payload for saving Facebook platform settings.
/// </summary>
public class FacebookApiRequest
{
    /// <summary>
    /// Gets or sets whether the platform is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Facebook page identifier.
    /// </summary>
    public string? PageId { get; set; }

    /// <summary>
    /// Gets or sets the Facebook app identifier.
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets the Facebook page access token.
    /// </summary>
    public string? PageAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the Facebook app secret.
    /// </summary>
    public string? AppSecret { get; set; }

    /// <summary>
    /// Gets or sets the Facebook client token.
    /// </summary>
    public string? ClientToken { get; set; }

    /// <summary>
    /// Gets or sets the Facebook short-lived access token.
    /// </summary>
    public string? ShortLivedAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the Facebook long-lived access token.
    /// </summary>
    public string? LongLivedAccessToken { get; set; }
}
