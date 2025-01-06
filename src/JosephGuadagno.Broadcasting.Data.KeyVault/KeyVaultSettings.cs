using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;

namespace JosephGuadagno.Broadcasting.Data.KeyVault;

/// <summary>
/// Settings for the KeyVault
/// </summary>
public class KeyVaultSettings: IKeyVaultSettings
{
    /// <summary>
    /// The KeyVault Uri
    /// </summary>
    public required string KeyVaultUri { get; set; }
    /// <summary>
    /// The tenant that the KeyVault resides in
    /// </summary>
    public required string TenantId { get; set; }
    /// <summary>
    /// The Client Id for accessing the KeyVault
    /// </summary>
    public required string ClientId { get; set; }
    /// <summary>
    /// The Client Secret for accessing the KeyVault
    /// </summary>
    public required string ClientSecret { get; set; }
}