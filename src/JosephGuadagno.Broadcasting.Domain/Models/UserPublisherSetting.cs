namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Per-user publisher configuration for a social media platform.
/// </summary>
public class UserPublisherSetting
{
    public int Id { get; set; }

    public string CreatedByEntraOid { get; set; } = string.Empty;

    public int SocialMediaPlatformId { get; set; }

    public string? SocialMediaPlatformName { get; set; }

    public SocialMediaPlatform? SocialMediaPlatform { get; set; }

    public bool IsEnabled { get; set; }

    public Dictionary<string, string?> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> WriteOnlyFields { get; set; } = [];

    public BlueskyPublisherSetting? Bluesky { get; set; }

    public TwitterPublisherSetting? Twitter { get; set; }

    public FacebookPublisherSetting? Facebook { get; set; }

    public LinkedInPublisherSetting? LinkedIn { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset LastUpdatedOn { get; set; }
}
