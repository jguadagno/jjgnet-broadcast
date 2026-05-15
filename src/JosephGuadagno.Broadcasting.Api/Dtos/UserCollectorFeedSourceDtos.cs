using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a per-user RSS/Atom feed source configuration.
/// Used by the user collector feed sources endpoints.
/// </summary>
public class UserCollectorFeedSourceRequest
{
    /// <summary>
    /// The URL of the RSS or Atom feed to poll for new content.
    /// Must be a valid absolute URL.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [Url]
    public string FeedUrl { get; set; } = string.Empty;

    /// <summary>
    /// The friendly display name for this feed configuration, used to identify it in the UI.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this feed configuration is currently active.
    /// Defaults to <c>true</c>; set to <c>false</c> to disable without deleting the record.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response DTO for a per-user RSS/Atom feed source configuration, returned by the
/// user collector feed sources endpoints.
/// </summary>
public class UserCollectorFeedSourceResponse
{
    /// <summary>
    /// The unique identifier of this feed source configuration record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The URL of the RSS or Atom feed being polled for new content.
    /// </summary>
    public string FeedUrl { get; set; } = string.Empty;

    /// <summary>
    /// The friendly display name for this feed configuration.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this feed configuration is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time when this configuration was first created, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// The date and time when this configuration was most recently updated, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who owns this feed configuration.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
}
