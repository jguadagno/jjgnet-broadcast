namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents per-user Twitter/X publisher settings
/// </summary>
public class UserPublisherTwitterSettings
{
    /// <summary>Gets or sets the unique identifier</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the Entra Object ID of the user who owns this configuration</summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>Gets or sets whether Twitter publishing is enabled for this user</summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether a consumer key is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-twitter-consumer-key</c>
    /// </summary>
    public bool HasConsumerKey { get; set; }

    /// <summary>
    /// Gets or sets whether a consumer secret is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-twitter-consumer-secret</c>
    /// </summary>
    public bool HasConsumerSecret { get; set; }

    /// <summary>
    /// Gets or sets whether an access token is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-twitter-access-token</c>
    /// </summary>
    public bool HasAccessToken { get; set; }

    /// <summary>
    /// Gets or sets whether an access token secret is stored in Key Vault for this user.
    /// KV secret name: <c>publisher-{ownerOid}-twitter-access-token-secret</c>
    /// </summary>
    public bool HasAccessTokenSecret { get; set; }

    /// <summary>Gets or sets when this configuration was created</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>Gets or sets when this configuration was last updated</summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
