using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserPublisherSettingManagerTests
{
    private readonly Mock<IUserPublisherSettingDataStore> _dataStore = new();
    private readonly Mock<ISocialMediaPlatformManager> _platformManager = new();
    private readonly UserPublisherSettingManager _sut;

    public UserPublisherSettingManagerTests()
    {
        _sut = new UserPublisherSettingManager(_dataStore.Object, _platformManager.Object);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldProjectWriteOnlyFieldsToPresenceFlags()
    {
        _dataStore
            .Setup(store => store.GetByUserAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new UserPublisherSetting
                {
                    Id = 1,
                    CreatedByEntraOid = "owner-1",
                    SocialMediaPlatformId = 4,
                    SocialMediaPlatform = new SocialMediaPlatform { Id = 4, Name = "LinkedIn", IsActive = true },
                    IsEnabled = true,
                    Settings = new Dictionary<string, string?>
                    {
                        ["AuthorId"] = "author-123",
                        ["ClientId"] = "client-123",
                        ["ClientSecret"] = "secret",
                        ["AccessToken"] = "token"
                    }
                }
            ]);

        var result = await _sut.GetByUserAsync("owner-1");

        result.Should().ContainSingle();
        result[0].LinkedIn.Should().NotBeNull();
        result[0].LinkedIn!.AuthorId.Should().Be("author-123");
        result[0].LinkedIn!.ClientId.Should().Be("client-123");
        result[0].LinkedIn!.HasClientSecret.Should().BeTrue();
        result[0].LinkedIn!.HasAccessToken.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_ShouldPreserveExistingSecretsWhenUpdateOmitsThem()
    {
        _platformManager
            .Setup(manager => manager.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SocialMediaPlatform { Id = 2, Name = "BlueSky", IsActive = true });
        _dataStore
            .Setup(store => store.GetByUserAndPlatformAsync("owner-1", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPublisherSetting
            {
                Id = 7,
                CreatedByEntraOid = "owner-1",
                SocialMediaPlatformId = 2,
                SocialMediaPlatform = new SocialMediaPlatform { Id = 2, Name = "BlueSky", IsActive = true },
                Settings = new Dictionary<string, string?>
                {
                    ["BlueskyUserName"] = "@switch",
                    ["BlueskyPassword"] = "saved-password"
                }
            });
        _dataStore
            .Setup(store => store.SaveAsync(It.IsAny<UserPublisherSetting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPublisherSetting setting, CancellationToken _) => setting);

        var result = await _sut.SaveAsync(new UserPublisherSettingUpdate
        {
            CreatedByEntraOid = "owner-1",
            SocialMediaPlatformId = 2,
            IsEnabled = true,
            Bluesky = new BlueskyPublisherSettingUpdate
            {
                UserName = "@updated"
            }
        });

        result.Should().NotBeNull();
        result!.Bluesky.Should().NotBeNull();
        result.Bluesky!.UserName.Should().Be("@updated");
        result.Bluesky.HasAppPassword.Should().BeTrue();
        _dataStore.Verify(
            store => store.SaveAsync(
                It.Is<UserPublisherSetting>(setting =>
                    setting.Settings["BlueskyUserName"] == "@updated"
                    && setting.Settings["BlueskyPassword"] == "saved-password"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenPlatformMissing_ShouldReturnNull()
    {
        _platformManager
            .Setup(manager => manager.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        var result = await _sut.SaveAsync(new UserPublisherSettingUpdate
        {
            CreatedByEntraOid = "owner-1",
            SocialMediaPlatformId = 999
        });

        result.Should().BeNull();
        _dataStore.Verify(store => store.SaveAsync(It.IsAny<UserPublisherSetting>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
