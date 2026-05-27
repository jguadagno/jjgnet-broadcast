using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>View model for the dispatchers aggregate page.</summary>
public class DispatchersAggregateViewModel
{
    public UserPlatformBlueskySettings? Bluesky { get; set; }
    public UserPlatformTwitterSettings? Twitter { get; set; }
    public UserPlatformLinkedInSettings? LinkedIn { get; set; }
    public UserPlatformFacebookSettings? Facebook { get; set; }

    /// <summary>Platform cards driven from the SocialMediaPlatform service (excludes platforms with no controller).</summary>
    public IReadOnlyList<DispatcherPlatformCardViewModel> Platforms { get; set; } = [];
}

/// <summary>View model for a single dispatcher platform card on the Dispatchers/Index page.</summary>
public class DispatcherPlatformCardViewModel
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Bootstrap icon class, e.g. "bi-twitter-x". The "bi" base class is added separately in HTML.</summary>
    public string Icon { get; init; } = string.Empty;

    public string Controller { get; init; } = string.Empty;
    public bool IsConfigured { get; init; }
    public bool IsEnabled { get; init; }
}

