using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents a per-user YouTube channel collector configuration
/// </summary>
public class UserCollectorYouTubeChannel
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the Entra Object ID of the user who owns this configuration
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the YouTube channel ID to poll
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the YouTube playlist ID to poll for new items.</summary>
    [StringLength(255)]
    public string PlaylistId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Azure Key Vault secret name that holds the Google API key for YouTube Data API access.</summary>
    [StringLength(255)]
    public string? ApiKeySecretName { get; set; }

    /// <summary>
    /// Gets or sets the raw Google API key. This is a transient field used only to pass the key
    /// through the Web→API layer. It is never stored in the database.
    /// </summary>
    [StringLength(255)]
    public string? ApiKey { get; set; }

    /// <summary>Gets or sets the maximum number of results to return per YouTube API page. Range: 1–200.</summary>
    public int ResultSetPageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the friendly display name for this channel
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether this channel configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets when this configuration was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
    
    /// <summary>
    /// Gets or sets when this configuration was last updated
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
