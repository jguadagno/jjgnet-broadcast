using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserPlatformFacebookSettingsDataStore — isolation and owner enforcement
/// </summary>
public class UserPlatformFacebookSettingsDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserPlatformFacebookSettingsDataStore>> _logger = new();
    private readonly UserPlatformFacebookSettingsDataStore _dataStore;

    public UserPlatformFacebookSettingsDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserPlatformSettingsMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserPlatformFacebookSettingsDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateFacebookSettingsAsync(
        string ownerOid,
        string? pageId = "page-123",
        string? appId = "app-456",
        bool isEnabled = true)
    {
        _context.UserPlatformFacebookSettings.Add(new UserPlatformFacebookSettings
        {
            CreatedByEntraOid = ownerOid,
            PageId = pageId,
            AppId = appId,
            IsEnabled = isEnabled,
            HasPageAccessToken = false,
            HasAppSecret = false,
            HasClientToken = false,
            HasShortLivedAccessToken = false,
            HasLongLivedAccessToken = false,
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsSettingsForThatUser()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateFacebookSettingsAsync(ownerOid, "page-abc", "app-xyz");

        // Act
        var result = await _dataStore.GetByUserAsync(ownerOid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("page-abc", result.PageId);
        Assert.Equal("app-xyz", result.AppId);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsNullWhenUserHasNoSettings()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateFacebookSettingsAsync(ownerAOid);

        // Act
        var result = await _dataStore.GetByUserAsync(ownerBOid);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSettingsById()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateFacebookSettingsAsync(ownerOid);

        var entity = await _context.UserPlatformFacebookSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);

        // Act
        var result = await _dataStore.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForMissingId()
    {
        // Act
        var result = await _dataStore.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewSettings()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        var newSettings = new Domain.Models.UserPlatformFacebookSettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            PageId = "new-page-id",
            AppId = "new-app-id",
            HasPageAccessToken = true,
            HasAppSecret = true,
            HasClientToken = false,
            HasShortLivedAccessToken = true,
            HasLongLivedAccessToken = false
        };

        // Act
        var result = await _dataStore.SaveAsync(newSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("new-page-id", result.PageId);
        Assert.True(result.HasPageAccessToken);
        Assert.True(result.HasShortLivedAccessToken);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSettingsForSameOwner()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateFacebookSettingsAsync(ownerOid, isEnabled: false);

        var updatedSettings = new Domain.Models.UserPlatformFacebookSettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            PageId = "updated-page",
            AppId = "updated-app",
            HasPageAccessToken = true,
            HasLongLivedAccessToken = true
        };

        // Act
        var result = await _dataStore.SaveAsync(updatedSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
        Assert.Equal("updated-page", result.PageId);
        Assert.True(result.HasLongLivedAccessToken);

        var count = await _context.UserPlatformFacebookSettings
            .CountAsync(s => s.CreatedByEntraOid == ownerOid);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenOwnerMatches()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateFacebookSettingsAsync(ownerOid);

        var entity = await _context.UserPlatformFacebookSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);
        var settingsId = entity.Id;

        // Act
        var result = await _dataStore.DeleteAsync(settingsId, ownerOid);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.UserPlatformFacebookSettings.FindAsync(settingsId));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenOwnerMismatch()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateFacebookSettingsAsync(ownerAOid);

        var entity = await _context.UserPlatformFacebookSettings.FirstAsync(s => s.CreatedByEntraOid == ownerAOid);
        var settingsId = entity.Id;

        // Act — user B attempts to delete user A's settings (MUST FAIL)
        var result = await _dataStore.DeleteAsync(settingsId, ownerBOid);

        // Assert
        Assert.False(result);
        var stillExists = await _context.UserPlatformFacebookSettings.FindAsync(settingsId);
        Assert.NotNull(stillExists);
        Assert.Equal(ownerAOid, stillExists.CreatedByEntraOid);
    }
}


