namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>Aggregated response containing all publisher settings for a user.</summary>
public class PublishersAggregateResponse
{
    /// <summary>Gets or sets the Bluesky publisher settings (null if not configured).</summary>
    public BlueskySettingsResponse? Bluesky { get; set; }

    /// <summary>Gets or sets the Twitter publisher settings (null if not configured).</summary>
    public TwitterSettingsResponse? Twitter { get; set; }

    /// <summary>Gets or sets the LinkedIn publisher settings (null if not configured).</summary>
    public LinkedInSettingsResponse? LinkedIn { get; set; }

    /// <summary>Gets or sets the Facebook publisher settings (null if not configured).</summary>
    public FacebookSettingsResponse? Facebook { get; set; }
}
