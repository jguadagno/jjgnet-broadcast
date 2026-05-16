using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>View model for the publishers aggregate page.</summary>
public class PublishersAggregateViewModel
{
    public UserPublisherBlueskySettings? Bluesky { get; set; }
    public UserPublisherTwitterSettings? Twitter { get; set; }
    public UserPublisherLinkedInSettings? LinkedIn { get; set; }
    public UserPublisherFacebookSettings? Facebook { get; set; }
}
