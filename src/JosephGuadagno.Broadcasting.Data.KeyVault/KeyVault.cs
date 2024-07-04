using Azure.Security.KeyVault.Secrets;
using JosephGuadagnoNet.Broadcasting.Data.KeyVault.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace JosephGuadagnoNet.Broadcasting.Data.KeyVault;

public class KeyVault: IKeyVault
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVault> _logger;
    private readonly TelemetryClient _telemetryClient;
    
    public KeyVault(SecretClient secretClient, ILogger<KeyVault> logger, TelemetryClient telemetryClient)
    {
        _secretClient = secretClient;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
    
    /// <summary>
    /// Updates the secret value and expiration date
    /// </summary>
    /// <param name="secretName">The name of the secret to update</param>
    /// <param name="secretValue">The value to update the secret to</param>
    /// <param name="expiresOn">The UTC datetime that this secret expires</param>
    /// <exception cref="ApplicationException">Thrown is any of the operations fail</exception>
    /// <remarks>
    /// This method will update the secret value and expiration date. It will also disable the old secret.
    /// </remarks>
    public async Task UpdateSecretValueAndPropertiesAsync(string secretName, string secretValue, DateTime expiresOn)
    {
        var originalSecretResponse = await _secretClient.GetSecretAsync(secretName);
        if (originalSecretResponse is null)
        {
            throw new ApplicationException($"Failed to get the secret '{secretName}' from the Key Vault");
        }
        var originalSecret = originalSecretResponse.Value;
        
        // Set the old secret to disabled
        originalSecret.Properties.Enabled = false;
        var updatePropertiesResponse = await _secretClient.UpdateSecretPropertiesAsync(originalSecret.Properties);
        if (updatePropertiesResponse is null)
        {
            throw new ApplicationException($"Failed to update the original version secret properties for '{secretName}'");
        }
        
        // Update secret value (create a new version)
        var newSecretVersionResponse = await _secretClient.SetSecretAsync(secretName, secretValue);
        if (newSecretVersionResponse is null)
        {
            throw new ApplicationException($"Failed to update the secret value for '{secretName}'");
        }
        var newKeyVaultSecretVersion = newSecretVersionResponse.Value;
        
        // Update the expiration date
        newKeyVaultSecretVersion.Properties.ExpiresOn = expiresOn;
        updatePropertiesResponse = await _secretClient.UpdateSecretPropertiesAsync(newKeyVaultSecretVersion.Properties);
        if (updatePropertiesResponse is null)
        {
            throw new ApplicationException($"Failed to update the new version secret properties for '{secretName}'");
        }
    }

    /// <summary>
    /// Gets the secret from the Key Vault
    /// </summary>
    /// <param name="secretName">The name of the secret</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown if the <param name="secretName"></param> is null or empty</exception>
    /// <exception cref="ApplicationException">Thrown if the response from KeyVault failed</exception>
    public async Task<KeyVaultSecret> GetSecretAsync(string secretName)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }
        var secretResponse = await _secretClient.GetSecretAsync(secretName);
        if (secretResponse is null)
        {
            throw new ApplicationException($"Failed to get the secret '{secretName}' from the Key Vault");
        }

        return secretResponse.Value;
    }
}