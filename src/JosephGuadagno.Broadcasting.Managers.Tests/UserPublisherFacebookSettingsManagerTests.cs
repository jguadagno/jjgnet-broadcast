using FluentAssertions;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserPublisherFacebookSettingsManagerTests
{
    private readonly Mock<IUserPublisherFacebookSettingsDataStore> _dataStore = new();
    private readonly Mock<IKeyVault> _keyVault = new();
    private readonly UserPublisherFacebookSettingsManager _sut;

    public UserPublisherFacebookSettingsManagerTests()
    {
        _sut = new UserPublisherFacebookSettingsManager(
            _dataStore.Object,
            _keyVault.Object,
            NullLogger<UserPublisherFacebookSettingsManager>.Instance);
    }

    [Fact]
    public async Task GetAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var expected = new UserPublisherFacebookSettings { CreatedByEntraOid = ownerOid, IsEnabled = true };
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
            .ReturnsAsync((UserPublisherFacebookSettings?)null);

        var result = await _sut.GetAsync("owner-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var settings = new UserPublisherFacebookSettings { CreatedByEntraOid = ownerOid, IsEnabled = true };
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
            .ReturnsAsync((UserPublisherFacebookSettings?)null);

        var result = await _sut.DeleteAsync("owner-1");

        result.Should().BeFalse();
        _dataStore.Verify(d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenSettingsExist()
    {
        const string ownerOid = "owner-1";
        var existing = new UserPublisherFacebookSettings { Id = 42, CreatedByEntraOid = ownerOid };
        _dataStore.Setup(d => d.GetByUserAsync(ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _dataStore.Setup(d => d.DeleteAsync(42, ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(ownerOid);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-facebook-page-access-token")]
    public async Task StorePageAccessTokenAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StorePageAccessTokenAsync(ownerOid, "my-page-token");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-page-token", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-facebook-app-secret")]
    public async Task StoreAppSecretAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreAppSecretAsync(ownerOid, "my-app-secret");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-app-secret", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-facebook-client-token")]
    public async Task StoreClientTokenAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreClientTokenAsync(ownerOid, "my-client-token");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-client-token", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-facebook-short-lived-access-token")]
    public async Task StoreShortLivedAccessTokenAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreShortLivedAccessTokenAsync(ownerOid, "my-short-token");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-short-token", It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("owner-1", "publisher-owner-1-facebook-long-lived-access-token")]
    public async Task StoreLongLivedAccessTokenAsync_BuildsCorrectSecretName(string ownerOid, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreLongLivedAccessTokenAsync(ownerOid, "my-long-token");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(expectedSecretName, "my-long-token", It.IsAny<DateTime>()), Times.Once);
    }
}
