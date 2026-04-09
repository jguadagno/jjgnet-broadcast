using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents a social media platform (Twitter, BlueSky, LinkedIn, Facebook, Mastodon, etc.)
/// </summary>
public class SocialMediaPlatform
{
    /// <summary>
    /// The unique identifier for the social media platform
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The name of the social media platform (e.g., Twitter, BlueSky, LinkedIn)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The canonical URL for the platform (e.g., https://twitter.com)
    /// </summary>
    [MaxLength(500)]
    [Url]
    public string? Url { get; set; }

    /// <summary>
    /// The Bootstrap icon class name for the platform (e.g., bi-twitter-x)
    /// </summary>
    [MaxLength(100)]
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates if the platform is active (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
