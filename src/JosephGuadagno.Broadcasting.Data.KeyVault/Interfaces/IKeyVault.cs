using Azure.Security.KeyVault.Secrets;

namespace JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;

public interface IKeyVault
{
    Task UpdateSecretValueAndPropertiesAsync(string secretName, string secretValue, DateTime expiresOn);
    Task<KeyVaultSecret> GetSecretAsync(string secretName);
}