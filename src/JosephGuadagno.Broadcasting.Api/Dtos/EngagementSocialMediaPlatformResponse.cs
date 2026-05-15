namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a social media platform associated with a speaking engagement,
/// returned by the engagement social media platform endpoints.
/// </summary>
public class EngagementSocialMediaPlatformResponse
{
    /// <summary>
    /// The unique identifier of the engagement this platform association belongs to.
    /// </summary>
    public int EngagementId { get; set; }

    /// <summary>
    /// The unique identifier of the social media platform associated with the engagement.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The engagement's social media handle or account identifier on the specified platform
    /// (e.g., <c>"@devconf"</c> on Twitter). Null if no handle was provided.
    /// </summary>
    public string? Handle { get; set; }

    /// <summary>
    /// The full social media platform details, including name, URL, and icon. May be null if not expanded.
    /// </summary>
    public SocialMediaPlatformResponse? SocialMediaPlatform { get; set; }
}
