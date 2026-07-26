namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API response payload for a social media platform.
/// </summary>
public class SocialMediaPlatformApiResponse
{
    /// <summary>
    /// Gets or sets the platform identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the platform name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the platform icon.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets whether the platform is active.
    /// </summary>
    public bool IsActive { get; set; }
}
