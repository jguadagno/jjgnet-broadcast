namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API request payload for adding a social media platform to an engagement.
/// </summary>
public class EngagementSocialMediaPlatformApiRequest
{
    /// <summary>
    /// Gets or sets the social media platform identifier.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the platform handle.
    /// </summary>
    public string? Handle { get; set; }
}
