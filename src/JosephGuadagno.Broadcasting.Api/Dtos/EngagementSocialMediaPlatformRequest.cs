using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for associating a social media platform with a speaking engagement.
/// Used by the <c>POST /engagements/{id}/social-media-platforms</c> endpoint.
/// </summary>
public class EngagementSocialMediaPlatformRequest
{
    /// <summary>
    /// The unique identifier of the social media platform to associate with the engagement.
    /// Must reference a valid, existing social media platform (greater than zero).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "SocialMediaPlatformId must be a valid platform identifier.")]
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The engagement's social media handle or account identifier on the specified platform
    /// (e.g., <c>"@devconf"</c> on Twitter). Maximum 200 characters. Optional.
    /// </summary>
    [MaxLength(200)]
    public string? Handle { get; set; }
}
