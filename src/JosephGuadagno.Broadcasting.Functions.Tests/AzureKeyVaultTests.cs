using System;
using System.Threading.Tasks;
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
        var secretName = "the-name-of-the-secret";
        var secretValue = DateTime.Now.ToString("s");
        var client = new SecretClient(new Uri(_settings.AzureKeyVaultUrl), new DefaultAzureCredential());
        
        // Act
        await client.SetSecretAsync(secretName, secretValue);
        
        var updatedSecret = await client.GetSecretAsync(secretName);
        // Assert
        Assert.NotNull(updatedSecret);
        Assert.Equal(secretValue, updatedSecret.Value.Value);
    }
}