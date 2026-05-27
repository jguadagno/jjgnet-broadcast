using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

/// <summary>
/// EF Core entity representing per-user Twitter/X publisher settings
/// </summary>
public class UserPlatformTwitterSettings
{
    /// <summary>Gets or sets the unique identifier</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the Entra Object ID of the user who owns this configuration</summary>
    [MaxLength(36)]
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>Gets or sets whether Twitter publishing is enabled for this user</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether a consumer key is stored in Key Vault for this user</summary>
    public bool HasConsumerKey { get; set; }

    /// <summary>Gets or sets whether a consumer secret is stored in Key Vault for this user</summary>
    public bool HasConsumerSecret { get; set; }

    /// <summary>Gets or sets whether an access token is stored in Key Vault for this user</summary>
    public bool HasAccessToken { get; set; }

    /// <summary>Gets or sets whether an access token secret is stored in Key Vault for this user</summary>
    public bool HasAccessTokenSecret { get; set; }

    /// <summary>Gets or sets when this configuration was created</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>Gets or sets when this configuration was last updated</summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

