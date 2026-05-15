namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a social media platform definition, returned by the social media platform endpoints.
/// </summary>
public class SocialMediaPlatformResponse
{
    /// <summary>
    /// The unique identifier of the social media platform record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The display name of the social media platform (e.g., <c>"Twitter"</c>, <c>"LinkedIn"</c>).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the social media platform's public website.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The CSS class name or icon identifier used to render the platform's logo in the UI.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates whether this social media platform is currently available for broadcasting.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// URL to documentation that describes how to obtain and configure API credentials for this platform.
    /// </summary>
    public string? CredentialSetupDocumentationUrl { get; set; }
}
