using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class AzureKeyVaultTests(SecretClient secretClient)
{
    private const string SecretName = "secret-for-unit-testing";

    [Fact]
    public async Task WriteSecretValue_WithValidSecret_ShouldWriteSecret()
    {
        // Arrange
        var secretValue = DateTime.Now.ToString("s");

        // Act
        await secretClient.SetSecretAsync(SecretName, secretValue, TestContext.Current.CancellationToken);
        
        var updatedSecret = await secretClient.GetSecretAsync(SecretName, cancellationToken: TestContext.Current.CancellationToken);
        // Assert
        Assert.NotNull(updatedSecret);
        Assert.Equal(secretValue, updatedSecret.Value.Value);
    }

    [Fact]
    public async Task UpdateSecretValueAndProperties_WithValidSecret_ShouldUpdateSecret()
    {
        // Arrange
        var secretValue = DateTime.Now.ToString("s");
        var expiresOn = DateTime.UtcNow.AddHours(1);
        
        // Act
        await UpdateSecretValueAndProperties(secretClient, SecretName, secretValue, expiresOn);
        
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