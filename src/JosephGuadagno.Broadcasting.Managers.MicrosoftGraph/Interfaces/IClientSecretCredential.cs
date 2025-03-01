namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;

public class IClientSecretCredential
{
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