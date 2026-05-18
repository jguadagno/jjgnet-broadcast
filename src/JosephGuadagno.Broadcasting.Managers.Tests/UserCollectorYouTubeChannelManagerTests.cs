using FluentAssertions;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserCollectorYouTubeChannelManagerTests
{
    private readonly Mock<IUserCollectorYouTubeChannelDataStore> _dataStore = new();
    private readonly Mock<IKeyVault> _keyVault = new();
    private readonly UserCollectorYouTubeChannelManager _sut;

    public UserCollectorYouTubeChannelManagerTests()
    {
        _sut = new UserCollectorYouTubeChannelManager(
            _dataStore.Object,
            _keyVault.Object,
            NullLogger<UserCollectorYouTubeChannelManager>.Instance);
    }

    [Theory]
    [InlineData("owner-1", "UCabc123", "collector-owner-1-youtube-channel-c52b99988c271f2d-api-key")]
    [InlineData("owner@with#special!", "UCabc123", "collector-owner-with-special--youtube-channel-c52b99988c271f2d-api-key")]
    public async Task StoreApiKeyToKeyVaultAsync_BuildsCorrectSecretName(
        string ownerOid, string channelId, string expectedSecretName)
    {
        _keyVault.Setup(k => k.UpdateSecretValueAndPropertiesAsync(
                expectedSecretName,
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await _sut.StoreApiKeyToKeyVaultAsync(ownerOid, channelId, "my-api-key");

        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(
            expectedSecretName,
            "my-api-key",
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task GetApiKeyAsync_ReturnsNullWhenConfigNotFound()
    {
        _dataStore.Setup(d => d.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCollectorYouTubeChannel?)null);

        var result = await _sut.GetApiKeyAsync("owner-1", 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var expected = new List<UserCollectorYouTubeChannel>
        {
            new() { CreatedByEntraOid = ownerOid, ChannelId = "UCabc123" }
        };
        _dataStore.Setup(d => d.GetByUserAsync(ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByUserAsync(ownerOid);

        result.Should().BeEquivalentTo(expected);
        _dataStore.Verify(d => d.GetByUserAsync(ownerOid, It.IsAny<CancellationToken>()), Times.Once);
    }
}
