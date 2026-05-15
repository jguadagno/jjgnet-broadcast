using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating a per-user YouTube channel configuration.
/// ApiKey is required on creation — a channel cannot be polled without a Google API key.
/// </summary>
public class CreateUserCollectorYouTubeChannelRequest
{
    /// <summary>The YouTube channel ID to poll for new video uploads (e.g., <c>"UCxxx"</c>).
    /// <remarks>This field is required.</remarks></summary>
    [Required]
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>The friendly display name for this channel configuration, used to identify it in the UI.
    /// <remarks>This field is required.</remarks></summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Indicates whether this channel configuration is currently active.
    /// Defaults to <c>true</c>; set to <c>false</c> to disable without deleting the record.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>The YouTube playlist ID to poll for new items. Leave empty to poll the channel's default uploads playlist.</summary>
    public string PlaylistId { get; set; } = string.Empty;

    /// <summary>The Google API key used to access the YouTube Data API for this channel.
    /// Stored securely in Azure Key Vault after creation.
    /// <remarks>This field is required.</remarks></summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The maximum number of results to return per YouTube Data API page request. Valid range: 1–200. Defaults to 50.</summary>
    [Range(1, 200)]
    public int ResultSetPageSize { get; set; } = 50;
}

/// <summary>
/// Request DTO for updating a per-user YouTube channel configuration.
/// ApiKey is optional — omit to keep the existing key already stored in Key Vault.
/// If no key is currently stored (HasApiKey == false on the existing record), ApiKey is required.
/// </summary>
public class UpdateUserCollectorYouTubeChannelRequest
{
    /// <summary>The YouTube channel ID to poll for new video uploads (e.g., <c>"UCxxx"</c>).
    /// <remarks>This field is required.</remarks></summary>
    [Required]
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>The friendly display name for this channel configuration, used to identify it in the UI.
    /// <remarks>This field is required.</remarks></summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Indicates whether this channel configuration is currently active.
    /// Defaults to <c>true</c>; set to <c>false</c> to disable without deleting the record.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>The YouTube playlist ID to poll for new items. Leave empty to poll the channel's default uploads playlist.</summary>
    public string PlaylistId { get; set; } = string.Empty;

    /// <summary>The Google API key used to access the YouTube Data API for this channel.
    /// Leave null or empty to keep the existing key stored in Azure Key Vault.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The maximum number of results to return per YouTube Data API page request. Valid range: 1–200. Defaults to 50.</summary>
    [Range(1, 200)]
    public int ResultSetPageSize { get; set; } = 50;
}

/// <summary>
/// Response DTO for a per-user YouTube channel configuration, returned by the
/// user collector YouTube channel endpoints. The API key is intentionally omitted from this response.
/// </summary>
public class UserCollectorYouTubeChannelResponse
{
    /// <summary>
    /// The unique identifier of this YouTube channel configuration record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The YouTube channel ID being polled for new video uploads.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// The friendly display name for this channel configuration.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this channel configuration is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>The YouTube playlist ID being polled for new items. Empty string indicates the channel's default uploads playlist.</summary>
    public string PlaylistId { get; set; } = string.Empty;
    // Note: ApiKey is intentionally EXCLUDED from the response DTO (never send API keys to clients)
    /// <summary>The maximum number of results returned per YouTube Data API page request (1–200).</summary>
    public int ResultSetPageSize { get; set; }

    /// <summary>
    /// Indicates whether a Google API key has been configured and stored in Azure Key Vault for this channel.
    /// </summary>
    public bool HasApiKey { get; set; }

    /// <summary>
    /// The date and time when this configuration was first created, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// The date and time when this configuration was most recently updated, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who owns this channel configuration.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
}
