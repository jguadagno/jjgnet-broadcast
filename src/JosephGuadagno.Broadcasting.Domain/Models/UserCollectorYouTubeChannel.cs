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
