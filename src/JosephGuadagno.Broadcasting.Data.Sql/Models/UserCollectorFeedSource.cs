namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

/// <summary>
/// EF Core entity representing a per-user RSS/Atom/JSON feed collector configuration
/// </summary>
public class UserCollectorFeedSource
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the Entra Object ID of the user who owns this config
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the feed URL to poll
    /// </summary>
    public string FeedUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the friendly display name for this feed
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether this feed config is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets when this config was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
    
    /// <summary>
    /// Gets or sets when this config was last updated
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
