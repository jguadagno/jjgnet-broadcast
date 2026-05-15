using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

/// <summary>
/// EF Core entity representing a per-user speaking engagements file collector configuration
/// </summary>
public class UserCollectorSpeakingEngagement
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Entra Object ID of the user who owns this config
    /// </summary>
    [MaxLength(100)]
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the JSON speaking engagements file
    /// </summary>
    [MaxLength(2048)]
    public string SpeakingEngagementsFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly display name for this configuration
    /// </summary>
    [MaxLength(255)]
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
