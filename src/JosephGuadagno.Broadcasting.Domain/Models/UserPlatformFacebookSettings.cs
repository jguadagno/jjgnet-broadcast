namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents per-user Facebook publisher settings
/// </summary>
public class UserPlatformFacebookSettings
{
    /// <summary>Gets or sets the unique identifier</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the Entra Object ID of the user who owns this configuration</summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>Gets or sets whether Facebook publishing is enabled for this user</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the Facebook Page ID</summary>
    public string? PageId { get; set; }

    /// <summary>Gets or sets the Facebook App ID</summary>
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets whether a page access token is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-facebook-page-access-token</c>
    /// </summary>
    public bool HasPageAccessToken { get; set; }

    /// <summary>
    /// Gets or sets whether an app secret is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-facebook-app-secret</c>
    /// </summary>
    public bool HasAppSecret { get; set; }

    /// <summary>
    /// Gets or sets whether a client token is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-facebook-client-token</c>
    /// </summary>
    public bool HasClientToken { get; set; }

    /// <summary>
    /// Gets or sets whether a short-lived access token is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-facebook-short-lived-access-token</c>
    /// </summary>
    public bool HasShortLivedAccessToken { get; set; }

    /// <summary>
    /// Gets or sets whether a long-lived access token is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-facebook-long-lived-access-token</c>
    /// </summary>
    public bool HasLongLivedAccessToken { get; set; }

    /// <summary>Gets or sets when this configuration was created</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>Gets or sets when this configuration was last updated</summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

