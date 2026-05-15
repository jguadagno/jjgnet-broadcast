using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a social media platform definition. Used by the
/// <c>POST /social-media-platforms</c> and <c>PUT /social-media-platforms/{id}</c> endpoints.
/// </summary>
public class SocialMediaPlatformRequest
{
    /// <summary>
    /// The display name of the social media platform (e.g., <c>"Twitter"</c>, <c>"LinkedIn"</c>).
    /// Maximum 100 characters.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the social media platform's public website. Must be a valid absolute URL.
    /// Maximum 500 characters. Optional.
    /// </summary>
    [MaxLength(500)]
    [Url]
    public string? Url { get; set; }

    /// <summary>
    /// The CSS class name or icon identifier used to render the platform's logo in the UI
    /// (e.g., a Font Awesome class like <c>"fa-brands fa-x-twitter"</c>). Maximum 100 characters. Optional.
    /// </summary>
    [MaxLength(100)]
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates whether this social media platform is currently available for broadcasting.
    /// Defaults to <c>true</c>; set to <c>false</c> to disable without deleting the record.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// URL pointing to documentation that describes how to obtain and configure API credentials
    /// for this platform. Maximum 500 characters. Optional.
    /// </summary>
    [MaxLength(500)]
    public string? CredentialSetupDocumentationUrl { get; set; }
}
