using FluentAssertions;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserPlatformTwitterSettingsManagerTests
{
    private readonly Mock<IUserPlatformTwitterSettingsDataStore> _dataStore = new();
    private readonly Mock<IKeyVault> _keyVault = new();
    private readonly UserPlatformTwitterSettingsManager _sut;

    public UserPlatformTwitterSettingsManagerTests()
    {
        _sut = new UserPlatformTwitterSettingsManager(
            _dataStore.Object,
            _keyVault.Object,
            NullLogger<UserPlatformTwitterSettingsManager>.Instance);
    }

    [Fact]
    public async Task GetAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var expected = new UserPlatformTwitterSettings { CreatedByEntraOid = ownerOid, IsEnabled = true };
        _dataStore.Setup(d => d.GetByUserAsync(ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAsync(ownerOid);

        result.Should().Be(expected);
        _dataStore.Verify(d => d.GetByUserAsync(ownerOid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenNoSettings()
    {
        _dataStore.Setup(d => d.GetByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPlatformTwitterSettings?)null);

        var result = await _sut.GetAsync("owner-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var settings = new UserPlatformTwitterSettings { CreatedByEntraOid = ownerOid, IsEnabled = true };
        _dataStore.Setup(d => d.SaveAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await _sut.SaveAsync(settings);

        result.Should().Be(settings);
        _dataStore.Verify(d => d.SaveAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenNoExistingSettings()
    {
        _dataStore.Setup(d => d.GetByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPlatformTwitterSettings?)null);

        var result = await _sut.DeleteAsync("owner-1");

        result.Should().BeFalse();
        _dataStore.Verify(d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenSettingsExist()
    {
        const string ownerOid = "owner-1";
        var existing = new UserPlatformTwitterSettings { Id = 42, CreatedByEntraOid = ownerOid };
        _dataStore.Setup(d => d.GetByUserAsync(ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _dataStore.Setup(d => d.DeleteAsync(42, ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(ownerOid);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-twitter-consumer-key")]
    public async Task StoreConsumerKeyAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreConsumerKeyAsync(ownerOid, "my-consumer-key");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-consumer-key", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-twitter-consumer-secret")]
    public async Task StoreConsumerSecretAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreConsumerSecretAsync(ownerOid, "my-consumer-secret");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-consumer-secret", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-twitter-access-token")]
    public async Task StoreAccessTokenAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreAccessTokenAsync(ownerOid, "my-access-token");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-access-token", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-twitter-access-token-secret")]
    public async Task StoreAccessTokenSecretAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreAccessTokenSecretAsync(ownerOid, "my-access-token-secret");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-access-token-secret", It.IsAny<DateTime>()), Times.Once);
    }
}


