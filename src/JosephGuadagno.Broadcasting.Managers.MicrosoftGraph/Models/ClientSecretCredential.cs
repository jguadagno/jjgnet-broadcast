using JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Models;

/// <summary>
/// Represents settings for using the ClientSecret Credentials
/// </summary>
public class ClientSecretCredential: IClientSecretCredential
{
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