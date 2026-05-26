namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents a per-user event-to-publisher routing configuration.
/// </summary>
public class UserEventPublisherMapping
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Entra Object ID of the user who owns this configuration.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type that should route to the configured publisher.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the social media platform identifier to publish to.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets whether this event publisher mapping is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when this configuration was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
