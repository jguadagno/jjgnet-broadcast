using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a per-user feed source configuration
/// </summary>
public class UserCollectorFeedSourceRequest
{
    /// <summary>
    /// Gets or sets the feed URL to poll
    /// </summary>
    [Required]
    [Url]
    public string FeedUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly display name for this feed
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this feed configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response DTO for a per-user feed source configuration
/// </summary>
public class UserCollectorFeedSourceResponse
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the feed URL to poll
    /// </summary>
    public string FeedUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly display name for this feed
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this feed configuration is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was last updated
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
