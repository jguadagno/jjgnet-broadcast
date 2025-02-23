using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class AzureKeyVaultTests(
    ISettings settings)
{
    private readonly string _secretName = "secret-for-unit-testing";

    [Fact]
    public void ValidateApplicationSettings()
    {
        // Arrange

        // Act

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.KeyVault);
        
        Assert.NotNull(settings.KeyVault.KeyVaultUri);
        Assert.False(string.IsNullOrEmpty(settings.KeyVault.KeyVaultUri));
        
        Assert.NotNull(settings.KeyVault.TenantId);
        Assert.False(string.IsNullOrEmpty(settings.KeyVault.TenantId));
        
        Assert.NotNull(settings.KeyVault.ClientId);
        Assert.False(string.IsNullOrEmpty(settings.KeyVault.ClientId));
        
        Assert.NotNull(settings.KeyVault.ClientSecret);
        Assert.False(string.IsNullOrEmpty(settings.KeyVault.ClientSecret));
    }

    [Fact]
    public async Task WriteSecretValue_WithValidSecret_ShouldWriteSecret()
    {
        // Arrange
        
        var secretValue = DateTime.Now.ToString("s");
        var client = new SecretClient(new Uri(settings.KeyVault.KeyVaultUri),
            new ChainedTokenCredential(new ManagedIdentityCredential(),
                new ClientSecretCredential(settings.KeyVault.TenantId, settings.KeyVault.ClientId,
                    settings.KeyVault.ClientSecret)));
        
        // Act
        await client.SetSecretAsync(_secretName, secretValue);
        
        var updatedSecret = await client.GetSecretAsync(_secretName);
        // Assert
        Assert.NotNull(updatedSecret);
        Assert.Equal(secretValue, updatedSecret.Value.Value);
    }

    [Fact]
    public async Task UpdateSecretValueAndProperties_WithValidSecret_ShouldUpdateSecret()
    {
        // Arrange
        var secretValue = DateTime.Now.ToString("s");
        var client = new SecretClient(new Uri(settings.KeyVault.KeyVaultUri),
            new ChainedTokenCredential(new ManagedIdentityCredential(),
                new ClientSecretCredential(settings.KeyVault.TenantId, settings.KeyVault.ClientId,
                    settings.KeyVault.ClientSecret)));
        var expiresOn = DateTime.UtcNow.AddHours(1);
        
        // Act
        await UpdateSecretValueAndProperties(client, _secretName, secretValue, expiresOn);
        
        // Assert
        Assert.True(true);
    }

    private async Task UpdateSecretValueAndProperties(SecretClient client, string secretName, string secretValue, DateTime expiresOn)
    {
        var originalSecretResponse = await client.GetSecretAsync(secretName);
        if (originalSecretResponse is null)
        {
            throw new ApplicationException($"Failed to get the secret '{secretName}' from the Key Vault");
        }
        var originalSecret = originalSecretResponse.Value;
        
        // Set the old secret to disabled
        originalSecret.Properties.Enabled = false;
        var updatePropertiesResponse = await client.UpdateSecretPropertiesAsync(originalSecret.Properties);
        if (updatePropertiesResponse is null)
        {
            throw new ApplicationException($"Failed to update the original version secret properties for '{secretName}'");
        }
        
        // Update secret value (create a new version)
        var newSecretVersionResponse = await client.SetSecretAsync(secretName, secretValue);
        if (newSecretVersionResponse is null)
        {
            throw new ApplicationException($"Failed to update the secret value for '{secretName}'");
        }
        var newKeyVaultSecretVersion = newSecretVersionResponse.Value;
        
        // Update the expiration date
        newKeyVaultSecretVersion.Properties.ExpiresOn = expiresOn;
        updatePropertiesResponse = await client.UpdateSecretPropertiesAsync(newKeyVaultSecretVersion.Properties);
        if (updatePropertiesResponse is null)
        {
            throw new ApplicationException($"Failed to update the new version secret properties for '{secretName}'");
        }
    }
}