using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for adding a social media platform to an engagement.
/// </summary>
public class EngagementSocialMediaPlatformRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "SocialMediaPlatformId must be a valid platform identifier.")]
    public int SocialMediaPlatformId { get; set; }

    [MaxLength(200)]
    public string? Handle { get; set; }
}
