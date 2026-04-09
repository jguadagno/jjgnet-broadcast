namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for an engagement's social media platform.
/// </summary>
public class EngagementSocialMediaPlatformResponse
{
    public int EngagementId { get; set; }
    public int SocialMediaPlatformId { get; set; }
    public string? Handle { get; set; }
    public SocialMediaPlatformResponse? SocialMediaPlatform { get; set; }
}
