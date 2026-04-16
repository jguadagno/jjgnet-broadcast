namespace JosephGuadagno.Broadcasting.Domain.Exceptions;

/// <summary>
/// Thrown when an engagement already has an association for the requested social media platform.
/// </summary>
public sealed class DuplicateEngagementSocialMediaPlatformException(int engagementId, int socialMediaPlatformId)
    : BroadcastingException(
        $"Engagement {engagementId} already has social media platform {socialMediaPlatformId} assigned.")
{
    public int EngagementId { get; } = engagementId;

    public int SocialMediaPlatformId { get; } = socialMediaPlatformId;
}
