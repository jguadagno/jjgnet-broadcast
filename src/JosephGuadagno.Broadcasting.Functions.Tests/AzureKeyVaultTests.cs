using System;
using System.Threading.Tasks;
using System.Xml;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class AzureKeyVaultTests
{

    private readonly ISettings _settings;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger<AzureKeyVaultTests> _logger;
    
    private readonly string _secretName = "secret-for-unit-testing";

    public AzureKeyVaultTests(ISettings settings, ITestOutputHelper testOutputHelper,
        ILogger<AzureKeyVaultTests> logger)
    {
        _settings = settings;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }

    [Fact]
    public void ValidateEnvironmentVariables()
    {
        // Arrange

        // Act

        // Assert
        Assert.NotNull(_settings.AzureKeyVaultUrl);
        Assert.False(string.IsNullOrEmpty(_settings.AzureKeyVaultUrl));
        Assert.NotNull(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"));
        Assert.False(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")));
        Assert.NotNull(Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"));
        Assert.False(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")));
        Assert.NotNull(Environment.GetEnvironmentVariable("AZURE_TENANT_ID"));
        Assert.False(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_TENANT_ID")));
    }

    [Fact]
    public async Task WriteSecretValue_WithValidSecret_ShouldWriteSecret()
    {
        // Arrange
        
        var secretValue = DateTime.Now.ToString("s");
        var client = new SecretClient(new Uri(_settings.AzureKeyVaultUrl), new DefaultAzureCredential());
        
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
        var client = new SecretClient(new Uri(_settings.AzureKeyVaultUrl), new DefaultAzureCredential());
        var expiresOn = DateTime.UtcNow.AddHours(1);
        
        // Act
        await UpdateSecretValueAndProperties(client, _secretName, secretValue, expiresOn);
        
        // Assert
        Assert.True(true);
    }
    
    public async Task UpdateSecretValueAndProperties(SecretClient client, string secretName, string secretValue, DateTime expiresOn)
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