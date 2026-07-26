namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents per-user Bluesky publisher settings
/// </summary>
public class UserPlatformBlueskySettings
{
    /// <summary>Gets or sets the unique identifier</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the Entra Object ID of the user who owns this configuration</summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>Gets or sets whether Bluesky publishing is enabled for this user</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the Bluesky handle/username</summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets whether an app password is stored in Key Vault for this user.
    /// The actual secret is never persisted in the database; it is stored in Key Vault under
    /// <c>publisher-{ownerOid}-bluesky-app-password</c>.
    /// </summary>
    public bool HasAppPassword { get; set; }

    /// <summary>Gets or sets when this configuration was created</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>Gets or sets when this configuration was last updated</summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

