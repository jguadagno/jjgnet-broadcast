namespace JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;

/// <summary>
/// The settings for the KeyVault.
/// </summary>
public interface IKeyVaultSettings
{
    /// <summary>
    /// The KeyVault Uri
    /// </summary>
    public string KeyVaultUri { get; set; }
    /// <summary>
    /// The tenant that the KeyVault resides in
    /// </summary>
    public string TenantId { get; set; }
    /// <summary>
    /// The Client Id for accessing the KeyVault
    /// </summary>
    public string ClientId { get; set; }
    /// <summary>
    /// The Client Secret for accessing the KeyVault
    /// </summary>
    public string ClientSecret { get; set; }
}