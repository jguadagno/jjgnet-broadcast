namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API request payload for creating or updating a user event distributor mapping.
/// </summary>
public class EventDistributorMappingApiRequest
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the social media platform identifier.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets whether the mapping is active.
    /// </summary>
    public bool IsActive { get; set; }
}
