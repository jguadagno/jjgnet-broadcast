using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>View model for the publishers aggregate page.</summary>
public class PublishersAggregateViewModel
{
    public UserPublisherBlueskySettings? Bluesky { get; set; }
    public UserPublisherTwitterSettings? Twitter { get; set; }
    public UserPublisherLinkedInSettings? LinkedIn { get; set; }
    public UserPublisherFacebookSettings? Facebook { get; set; }

    /// <summary>Platform cards driven from the SocialMediaPlatform service (excludes platforms with no controller).</summary>
    public IReadOnlyList<PublisherPlatformCardViewModel> Platforms { get; set; } = [];
}

/// <summary>View model for a single publisher platform card on the Publishers/Index page.</summary>
public class PublisherPlatformCardViewModel
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Bootstrap icon class, e.g. "bi-twitter-x". The "bi" base class is added separately in HTML.</summary>
    public string Icon { get; init; } = string.Empty;

    public string Controller { get; init; } = string.Empty;
    public bool IsConfigured { get; init; }
    public bool IsEnabled { get; init; }
}
