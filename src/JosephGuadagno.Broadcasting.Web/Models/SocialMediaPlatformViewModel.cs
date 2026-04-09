using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for a social media platform
/// </summary>
public class SocialMediaPlatformViewModel
{
    /// <summary>
    /// The unique identifier for the social media platform
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the social media platform (e.g., Twitter, BlueSky, LinkedIn)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The canonical URL for the platform (e.g., https://twitter.com)
    /// </summary>
    [Url(ErrorMessage = "Please enter a valid URL")]
    [MaxLength(500, ErrorMessage = "URL cannot exceed 500 characters")]
    public string? Url { get; set; }

    /// <summary>
    /// The Bootstrap icon class name for the platform (e.g., bi-twitter-x)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Icon class cannot exceed 100 characters")]
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates if the platform is active (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
