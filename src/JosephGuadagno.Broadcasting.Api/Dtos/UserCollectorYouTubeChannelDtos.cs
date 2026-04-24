using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a per-user YouTube channel configuration
/// </summary>
public class UserCollectorYouTubeChannelRequest
{
    /// <summary>
    /// Gets or sets the YouTube channel ID to poll
    /// </summary>
    [Required]
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly display name for this channel
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this channel configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response DTO for a per-user YouTube channel configuration
/// </summary>
public class UserCollectorYouTubeChannelResponse
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the YouTube channel ID to poll
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly display name for this channel
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this channel configuration is active
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
