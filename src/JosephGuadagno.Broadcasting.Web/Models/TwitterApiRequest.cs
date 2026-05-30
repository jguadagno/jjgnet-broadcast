namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API request payload for saving Twitter platform settings.
/// </summary>
public class TwitterApiRequest
{
    /// <summary>
    /// Gets or sets whether the platform is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Twitter consumer key.
    /// </summary>
    public string? ConsumerKey { get; set; }

    /// <summary>
    /// Gets or sets the Twitter consumer secret.
    /// </summary>
    public string? ConsumerSecret { get; set; }

    /// <summary>
    /// Gets or sets the Twitter access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the Twitter access token secret.
    /// </summary>
    public string? AccessTokenSecret { get; set; }
}
