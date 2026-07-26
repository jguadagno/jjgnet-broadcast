using FluentAssertions;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserPlatformLinkedInSettingsManagerTests
{
    private readonly Mock<IUserPlatformLinkedInSettingsDataStore> _dataStore = new();
    private readonly Mock<IKeyVault> _keyVault = new();
    private readonly UserPlatformLinkedInSettingsManager _sut;

    public UserPlatformLinkedInSettingsManagerTests()
    {
        _sut = new UserPlatformLinkedInSettingsManager(
            _dataStore.Object,
            _keyVault.Object,
            NullLogger<UserPlatformLinkedInSettingsManager>.Instance);
    }

    [Fact]
    public async Task GetAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var expected = new UserPlatformLinkedInSettings { CreatedByEntraOid = ownerOid, IsEnabled = true };
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
            .ReturnsAsync((UserPlatformLinkedInSettings?)null);

        var result = await _sut.GetAsync("owner-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var settings = new UserPlatformLinkedInSettings { CreatedByEntraOid = ownerOid, IsEnabled = true };
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
            .ReturnsAsync((UserPlatformLinkedInSettings?)null);

        var result = await _sut.DeleteAsync("owner-1");

        result.Should().BeFalse();
        _dataStore.Verify(d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenSettingsExist()
    {
        const string ownerOid = "owner-1";
        var existing = new UserPlatformLinkedInSettings { Id = 42, CreatedByEntraOid = ownerOid };
        _dataStore.Setup(d => d.GetByUserAsync(ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _dataStore.Setup(d => d.DeleteAsync(42, ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(ownerOid);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-linkedin-client-secret")]
    public async Task StoreClientSecretAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreClientSecretAsync(ownerOid, "my-client-secret");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-client-secret", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-linkedin-access-token")]
    public async Task StoreAccessTokenAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreAccessTokenAsync(ownerOid, "my-access-token");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-access-token", It.IsAny<DateTime>()), Times.Once);
    }
}


