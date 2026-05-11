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

    /// <summary>Gets or sets the YouTube playlist ID to poll for new items.</summary>
    public string PlaylistId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Google API key for YouTube Data API access.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Gets or sets the maximum results per YouTube API page (1–200).</summary>
    [Range(1, 200)]
    public int ResultSetPageSize { get; set; } = 50;
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

    public string PlaylistId { get; set; } = string.Empty;
    // Note: ApiKey is intentionally EXCLUDED from the response DTO (never send API keys to clients)
    public int ResultSetPageSize { get; set; }

    /// <summary>
    /// Gets or sets whether a Google API key has been configured for this channel.
    /// </summary>
    public bool HasApiKey { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was last updated
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
