using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Junction table linking Engagements to their social media platforms and handles
/// </summary>
public class EngagementSocialMediaPlatform
{
    /// <summary>
    /// The ID of the engagement
    /// </summary>
    [Required]
    public int EngagementId { get; set; }

    /// <summary>
    /// The ID of the social media platform
    /// </summary>
    [Required]
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The handle/username for this engagement on the platform (e.g., @NDCSydney, #DevConf2024)
    /// </summary>
    [MaxLength(200)]
    public string? Handle { get; set; }

    /// <summary>
    /// Navigation property to the engagement
    /// </summary>
    public Engagement? Engagement { get; set; }

    /// <summary>
    /// Navigation property to the social media platform
    /// </summary>
    public SocialMediaPlatform? SocialMediaPlatform { get; set; }
}
