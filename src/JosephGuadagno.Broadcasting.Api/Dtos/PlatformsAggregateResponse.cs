namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>Aggregated response containing all platform settings for a user.</summary>
public class PlatformsAggregateResponse
{
    /// <summary>Gets or sets the Bluesky platform settings (null if not configured).</summary>
    public BlueskySettingsResponse? Bluesky { get; set; }

    /// <summary>Gets or sets the Twitter platform settings (null if not configured).</summary>
    public TwitterSettingsResponse? Twitter { get; set; }

    /// <summary>Gets or sets the LinkedIn platform settings (null if not configured).</summary>
    public LinkedInSettingsResponse? LinkedIn { get; set; }

    /// <summary>Gets or sets the Facebook platform settings (null if not configured).</summary>
    public FacebookSettingsResponse? Facebook { get; set; }
}
