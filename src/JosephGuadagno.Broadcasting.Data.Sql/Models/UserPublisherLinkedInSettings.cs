using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

/// <summary>
/// EF Core entity representing per-user LinkedIn publisher settings
/// </summary>
public class UserPublisherLinkedInSettings
{
    /// <summary>Gets or sets the unique identifier</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the Entra Object ID of the user who owns this configuration</summary>
    [MaxLength(36)]
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>Gets or sets whether LinkedIn publishing is enabled for this user</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the LinkedIn author URN (person ID)</summary>
    [MaxLength(255)]
    public string? AuthorId { get; set; }

    /// <summary>Gets or sets the LinkedIn app client ID</summary>
    [MaxLength(255)]
    public string? ClientId { get; set; }

    /// <summary>Gets or sets whether a client secret is stored in Key Vault for this user</summary>
    public bool HasClientSecret { get; set; }

    /// <summary>Gets or sets when this configuration was created</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>Gets or sets when this configuration was last updated</summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
