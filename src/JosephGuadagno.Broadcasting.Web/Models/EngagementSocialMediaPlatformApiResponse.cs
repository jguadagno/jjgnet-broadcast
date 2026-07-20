namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API response payload for an engagement social media platform.
/// </summary>
public class EngagementSocialMediaPlatformApiResponse
{
    /// <summary>
    /// Gets or sets the engagement identifier.
    /// </summary>
    public int EngagementId { get; set; }

    /// <summary>
    /// Gets or sets the social media platform identifier.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the platform handle.
    /// </summary>
    public string? Handle { get; set; }

    /// <summary>
    /// Gets or sets the social media platform details.
    /// </summary>
    public SocialMediaPlatformApiResponse? SocialMediaPlatform { get; set; }
}
