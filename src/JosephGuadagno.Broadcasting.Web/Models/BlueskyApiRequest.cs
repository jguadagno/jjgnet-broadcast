namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API request payload for saving Bluesky platform settings.
/// </summary>
public class BlueskyApiRequest
{
    /// <summary>
    /// Gets or sets whether the platform is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Bluesky user name.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the Bluesky app password.
    /// </summary>
    public string? AppPassword { get; set; }
}
