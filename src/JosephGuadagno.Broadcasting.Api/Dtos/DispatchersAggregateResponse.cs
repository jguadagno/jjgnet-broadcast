namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>Aggregated response containing all dispatcher settings for a user.</summary>
public class DispatchersAggregateResponse
{
    /// <summary>Gets or sets the Bluesky dispatcher settings (null if not configured).</summary>
    public BlueskySettingsResponse? Bluesky { get; set; }

    /// <summary>Gets or sets the Twitter dispatcher settings (null if not configured).</summary>
    public TwitterSettingsResponse? Twitter { get; set; }

    /// <summary>Gets or sets the LinkedIn dispatcher settings (null if not configured).</summary>
    public LinkedInSettingsResponse? LinkedIn { get; set; }

    /// <summary>Gets or sets the Facebook dispatcher settings (null if not configured).</summary>
    public FacebookSettingsResponse? Facebook { get; set; }
}
