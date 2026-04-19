using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class UserPublisherSettingDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserPublisherSettingDataStore>> _logger = new();
    private readonly UserPublisherSettingDataStore _dataStore;

    public UserPublisherSettingDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserPublisherSettingDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<int> CreatePlatformAsync(string name = "BlueSky")
    {
        var platform = new SocialMediaPlatform
        {
            Name = name,
            Url = $"https://{name.ToLowerInvariant()}.com",
            Icon = $"bi-{name.ToLowerInvariant()}",
            IsActive = true
        };

        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();
        return platform.Id;
    }

    private async Task CreateSettingAsync(string ownerOid, int platformId, string? settingsJson, bool isEnabled = true)
    {
        _context.UserPublisherSettings.Add(new UserPublisherSetting
        {
            CreatedByEntraOid = ownerOid,
            SocialMediaPlatformId = platformId,
            IsEnabled = isEnabled,
            Settings = settingsJson,
            CreatedOn = DateTimeOffset.UtcNow.AddMinutes(-10),
            LastUpdatedOn = DateTimeOffset.UtcNow.AddMinutes(-10)
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyMatchingOwnerSettings()
    {
        var blueskyId = await CreatePlatformAsync("BlueSky");
        var linkedInId = await CreatePlatformAsync("LinkedIn");

        await CreateSettingAsync("owner-a", blueskyId, """{"BlueskyUserName":"@a","BlueskyPassword":"secret"}""");
        await CreateSettingAsync("owner-a", linkedInId, """{"AuthorId":"author-a","ClientId":"client-a","AccessToken":"token"}""");
        await CreateSettingAsync("owner-b", blueskyId, """{"BlueskyUserName":"@b"}""");

        var result = await _dataStore.GetByUserAsync("owner-a");

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("owner-a", item.CreatedByEntraOid));
        Assert.Contains(result, item => item.Bluesky?.HasAppPassword == true);
        Assert.Contains(result, item => item.LinkedIn?.AuthorId == "author-a");
    }

    [Fact]
    public async Task GetByUserAndPlatformAsync_ReturnsTypedPublisherProjection()
    {
        var platformId = await CreatePlatformAsync("Facebook");
        await CreateSettingAsync(
            "owner-a",
            platformId,
            """{"PageId":"123","AppId":"app-1","PageAccessToken":"token","AppSecret":"secret"}""");

        var result = await _dataStore.GetByUserAndPlatformAsync("owner-a", platformId);

        Assert.NotNull(result);
        Assert.Equal("Facebook", result!.SocialMediaPlatformName);
        Assert.Equal("123", result.Facebook?.PageId);
        Assert.Equal("app-1", result.Facebook?.AppId);
        Assert.True(result.Facebook?.HasPageAccessToken);
        Assert.True(result.Facebook?.HasAppSecret);
    }

    [Fact]
    public async Task SaveAsync_WhenSettingDoesNotExist_AddsSetting()
    {
        var platformId = await CreatePlatformAsync("Twitter");

        var result = await _dataStore.SaveAsync(new Domain.Models.UserPublisherSetting
        {
            CreatedByEntraOid = "owner-a",
            SocialMediaPlatformId = platformId,
            IsEnabled = true,
            Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConsumerKey"] = "ck",
                ["ConsumerSecret"] = "cs",
                ["OAuthToken"] = "ot",
                ["OAuthTokenSecret"] = "ots"
            }
        });

        Assert.NotNull(result);
        Assert.True(result!.Id > 0);
        Assert.True(result.Twitter?.HasConsumerKey);
        Assert.True(result.Twitter?.HasAccessTokenSecret);
        Assert.Single(_context.UserPublisherSettings);
    }

    [Fact]
    public async Task SaveAsync_WhenSettingExists_UpdatesExistingRow()
    {
        var platformId = await CreatePlatformAsync("BlueSky");
        await CreateSettingAsync("owner-a", platformId, """{"BlueskyUserName":"@old","BlueskyPassword":"secret"}""", isEnabled: false);

        var result = await _dataStore.SaveAsync(new Domain.Models.UserPublisherSetting
        {
            CreatedByEntraOid = "owner-a",
            SocialMediaPlatformId = platformId,
            IsEnabled = true,
            Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BlueskyUserName"] = "@new",
                ["BlueskyPassword"] = "new-secret"
            }
        });

        Assert.NotNull(result);
        Assert.True(result!.IsEnabled);
        Assert.Equal("@new", result.Bluesky?.UserName);
        Assert.True(result.Bluesky?.HasAppPassword);
        Assert.Single(_context.UserPublisherSettings);
    }

    [Fact]
    public async Task DeleteAsync_WhenSettingExists_RemovesRow()
    {
        var platformId = await CreatePlatformAsync("LinkedIn");
        await CreateSettingAsync("owner-a", platformId, """{"AuthorId":"author-a","ClientId":"client-a"}""");

        var deleted = await _dataStore.DeleteAsync("owner-a", platformId);

        Assert.True(deleted);
        Assert.Empty(_context.UserPublisherSettings);
    }

    [Fact]
    public async Task SaveAsync_WhenSaveFails_LogsSanitizedOwnerOidAndReturnsNull()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var mockContext = new Mock<BroadcastingContext>(options) { CallBase = true };
        mockContext
            .Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var logger = new Mock<ILogger<UserPublisherSettingDataStore>>();
        var dataStore = new UserPublisherSettingDataStore(mockContext.Object, mapperConfiguration.CreateMapper(), logger.Object);

        var result = await dataStore.SaveAsync(new Domain.Models.UserPublisherSetting
        {
            CreatedByEntraOid = "owner-\r\nspoof",
            SocialMediaPlatformId = 3,
            Settings = new Dictionary<string, string?>()
        });

        Assert.Null(result);
        VerifyLoggedOwnerWasSanitized(logger, "owner-spoof");
    }

    [Fact]
    public async Task DeleteAsync_WhenSaveFails_LogsSanitizedOwnerOidAndReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var seedContext = new BroadcastingContext(options))
        {
            seedContext.UserPublisherSettings.Add(new UserPublisherSetting
            {
                CreatedByEntraOid = "owner-\r\nspoof",
                SocialMediaPlatformId = 3,
                IsEnabled = true,
                CreatedOn = DateTimeOffset.UtcNow,
                LastUpdatedOn = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        var mockContext = new Mock<BroadcastingContext>(options) { CallBase = true };
        mockContext
            .Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var logger = new Mock<ILogger<UserPublisherSettingDataStore>>();
        var dataStore = new UserPublisherSettingDataStore(mockContext.Object, mapperConfiguration.CreateMapper(), logger.Object);

        var deleted = await dataStore.DeleteAsync("owner-\r\nspoof", 3);

        Assert.False(deleted);
        VerifyLoggedOwnerWasSanitized(logger, "owner-spoof");
    }

    private static void VerifyLoggedOwnerWasSanitized(Mock<ILogger<UserPublisherSettingDataStore>> logger, string sanitizedOwnerOid)
    {
        logger.Verify(
            target => target.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains(sanitizedOwnerOid, StringComparison.Ordinal)
                    && !state.ToString()!.Contains('\r')
                    && !state.ToString()!.Contains('\n')),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
