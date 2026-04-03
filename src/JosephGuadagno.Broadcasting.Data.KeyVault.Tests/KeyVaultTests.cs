using Azure;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.KeyVault.Tests;

public class KeyVaultTests
{
    private readonly Mock<SecretClient> _mockSecretClient;
    private readonly Mock<ILogger<KeyVault>> _mockLogger;
    private readonly KeyVault _keyVault;

    public KeyVaultTests()
    {
        _mockSecretClient = new Mock<SecretClient>();
        _mockLogger = new Mock<ILogger<KeyVault>>();
        _keyVault = new KeyVault(_mockSecretClient.Object, _mockLogger.Object);
    }

    private static Response<T> CreateResponse<T>(T value)
    {
        var mockRawResponse = new Mock<Response>();
        return Response.FromValue(value, mockRawResponse.Object);
    }

    #region GetSecretAsync Tests

    [Fact]
    public async Task GetSecretAsync_WithNullSecretName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _keyVault.GetSecretAsync(null!));
    }

    [Fact]
    public async Task GetSecretAsync_WithEmptySecretName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _keyVault.GetSecretAsync(string.Empty));
    }

    [Fact]
    public async Task GetSecretAsync_WithWhiteSpaceSecretName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _keyVault.GetSecretAsync("   "));
    }

    [Fact]
    public async Task GetSecretAsync_WhenClientReturnsNull_ShouldThrowApplicationException()
    {
        // Arrange
        _mockSecretClient
            .Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Response<KeyVaultSecret>)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => _keyVault.GetSecretAsync("my-secret"));
    }

    [Fact]
    public async Task GetSecretAsync_WithValidSecretName_ShouldReturnSecret()
    {
        // Arrange
        const string secretName = "my-secret";
        const string secretValue = "my-value";
        var keyVaultSecret = new KeyVaultSecret(secretName, secretValue);
        var response = CreateResponse(keyVaultSecret);

        _mockSecretClient
            .Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _keyVault.GetSecretAsync(secretName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secretName, result.Name);
        Assert.Equal(secretValue, result.Value);
        _mockSecretClient.Verify(
            c => c.GetSecretAsync(It.Is<string>(s => s == secretName), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateSecretValueAndPropertiesAsync Tests

    [Fact]
    public async Task UpdateSecretValueAndPropertiesAsync_WhenGetSecretReturnsNull_ShouldThrowApplicationException()
    {
        // Arrange
        _mockSecretClient
            .Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Response<KeyVaultSecret>)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            _keyVault.UpdateSecretValueAndPropertiesAsync("my-secret", "new-value", DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task UpdateSecretValueAndPropertiesAsync_WhenFirstUpdatePropertiesReturnsNull_ShouldThrowApplicationException()
    {
        // Arrange
        var keyVaultSecret = new KeyVaultSecret("my-secret", "old-value");
        var getSecretResponse = CreateResponse(keyVaultSecret);

        _mockSecretClient
            .Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getSecretResponse);
        _mockSecretClient
            .Setup(c => c.UpdateSecretPropertiesAsync(It.IsAny<SecretProperties>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Response<SecretProperties>)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            _keyVault.UpdateSecretValueAndPropertiesAsync("my-secret", "new-value", DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task UpdateSecretValueAndPropertiesAsync_WhenSetSecretReturnsNull_ShouldThrowApplicationException()
    {
        // Arrange
        var keyVaultSecret = new KeyVaultSecret("my-secret", "old-value");
        var getSecretResponse = CreateResponse(keyVaultSecret);
        var updatePropertiesResponse = CreateResponse(keyVaultSecret.Properties);

        _mockSecretClient
            .Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getSecretResponse);
        _mockSecretClient
            .Setup(c => c.UpdateSecretPropertiesAsync(It.IsAny<SecretProperties>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatePropertiesResponse);
        _mockSecretClient
            .Setup(c => c.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Response<KeyVaultSecret>)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            _keyVault.UpdateSecretValueAndPropertiesAsync("my-secret", "new-value", DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task UpdateSecretValueAndPropertiesAsync_WhenSecondUpdatePropertiesReturnsNull_ShouldThrowApplicationException()
    {
        // Arrange
        const string secretName = "my-secret";
        var originalSecret = new KeyVaultSecret(secretName, "old-value");
        var newSecret = new KeyVaultSecret(secretName, "new-value");
        var getSecretResponse = CreateResponse(originalSecret);
        var setSecretResponse = CreateResponse(newSecret);
        var updatePropertiesResponse = CreateResponse(originalSecret.Properties);

        _mockSecretClient
            .Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getSecretResponse);
        _mockSecretClient
            .SetupSequence(c => c.UpdateSecretPropertiesAsync(It.IsAny<SecretProperties>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatePropertiesResponse)
            .ReturnsAsync((Response<SecretProperties>)null!);
        _mockSecretClient
            .Setup(c => c.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(setSecretResponse);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, "new-value", DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task UpdateSecretValueAndPropertiesAsync_WithValidParameters_ShouldDisableOldVersionAndSetNewExpiration()
    {
        // Arrange
        const string secretName = "my-secret";
        const string newValue = "new-value";
        var expiresOn = DateTime.UtcNow.AddHours(1);
        var originalSecret = new KeyVaultSecret(secretName, "old-value");
        var newSecret = new KeyVaultSecret(secretName, newValue);
        var getSecretResponse = CreateResponse(originalSecret);
        var setSecretResponse = CreateResponse(newSecret);
        var updatePropertiesResponse = CreateResponse(originalSecret.Properties);

        _mockSecretClient
            .Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getSecretResponse);
        _mockSecretClient
            .Setup(c => c.UpdateSecretPropertiesAsync(It.IsAny<SecretProperties>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatePropertiesResponse);
        _mockSecretClient
            .Setup(c => c.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(setSecretResponse);

        // Act
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, newValue, expiresOn);

        // Assert
        Assert.False(originalSecret.Properties.Enabled);
        Assert.Equal(expiresOn, newSecret.Properties.ExpiresOn);
        _mockSecretClient.Verify(
            c => c.GetSecretAsync(It.Is<string>(s => s == secretName), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockSecretClient.Verify(
            c => c.UpdateSecretPropertiesAsync(It.IsAny<SecretProperties>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _mockSecretClient.Verify(
            c => c.SetSecretAsync(It.Is<string>(s => s == secretName), It.Is<string>(s => s == newValue), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
