namespace JosephGuadagno.Broadcasting.Web.Models.LinkedIn;

public class SavedTokenInfo
{
    public string AccessToken { get; set; }
    public string KeyVaultSecretName { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
}