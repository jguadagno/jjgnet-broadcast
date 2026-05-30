using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the aggregate platform settings returned by the Web API.
/// </summary>
public class PlatformsAggregateWebResponse
{
    /// <summary>
    /// Gets or sets the Bluesky settings.
    /// </summary>
    public UserPlatformBlueskySettings? Bluesky { get; set; }

    /// <summary>
    /// Gets or sets the Twitter settings.
    /// </summary>
    public UserPlatformTwitterSettings? Twitter { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn settings.
    /// </summary>
    public UserPlatformLinkedInSettings? LinkedIn { get; set; }

    /// <summary>
    /// Gets or sets the Facebook settings.
    /// </summary>
    public UserPlatformFacebookSettings? Facebook { get; set; }
}
