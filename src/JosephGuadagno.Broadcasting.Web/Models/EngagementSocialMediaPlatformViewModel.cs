namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model representing a social media platform associated with an engagement
/// </summary>
public class EngagementSocialMediaPlatformViewModel
{
    /// <summary>
    /// The identifier of the engagement
    /// </summary>
    public int EngagementId { get; set; }

    /// <summary>
    /// The identifier of the social media platform
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The handle or hashtag for this engagement on the platform
    /// </summary>
    public string? Handle { get; set; }

    /// <summary>
    /// The name of the social media platform
    /// </summary>
    public string? PlatformName { get; set; }

    /// <summary>
    /// The Bootstrap icon class for the platform
    /// </summary>
    public string? PlatformIcon { get; set; }
}
