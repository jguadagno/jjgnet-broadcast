using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents a per-user scheduled item publisher configuration
/// </summary>
public class UserCollectorScheduledItem
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Entra Object ID of the user who owns this configuration
    /// </summary>
    [StringLength(36)]
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly display name for this configuration
    /// </summary>
    [StringLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this configuration is active
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
