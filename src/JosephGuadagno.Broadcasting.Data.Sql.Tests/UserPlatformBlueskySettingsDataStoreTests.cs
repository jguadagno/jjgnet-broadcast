using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserPlatformBlueskySettingsDataStore — isolation and owner enforcement
/// </summary>
public class UserPlatformBlueskySettingsDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserPlatformBlueskySettingsDataStore>> _logger = new();
    private readonly UserPlatformBlueskySettingsDataStore _dataStore;

    public UserPlatformBlueskySettingsDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserPlatformSettingsMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserPlatformBlueskySettingsDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateBlueskySettingsAsync(
        string ownerOid,
        string? userName = "test.bsky.social",
        bool isEnabled = true,
        bool hasAppPassword = false)
    {
        _context.UserPlatformBlueskySettings.Add(new UserPlatformBlueskySettings
        {
            CreatedByEntraOid = ownerOid,
            UserName = userName,
            IsEnabled = isEnabled,
            HasAppPassword = hasAppPassword,
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
        await CreateBlueskySettingsAsync(ownerOid, "user-a.bsky.social");

        // Act
        var result = await _dataStore.GetByUserAsync(ownerOid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("user-a.bsky.social", result.UserName);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsNullWhenUserHasNoSettings()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateBlueskySettingsAsync(ownerAOid);

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
        await CreateBlueskySettingsAsync(ownerOid);

        var entity = await _context.UserPlatformBlueskySettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);

        // Act
        var result = await _dataStore.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
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
        var newSettings = new Domain.Models.UserPlatformBlueskySettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            UserName = "new.bsky.social",
            HasAppPassword = true
        };

        // Act
        var result = await _dataStore.SaveAsync(newSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("new.bsky.social", result.UserName);
        Assert.True(result.HasAppPassword);

        var persisted = await _context.UserPlatformBlueskySettings.FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSettingsForSameOwner()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateBlueskySettingsAsync(ownerOid, "original.bsky.social", isEnabled: false);

        var updatedSettings = new Domain.Models.UserPlatformBlueskySettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            UserName = "updated.bsky.social",
            HasAppPassword = true
        };

        // Act
        var result = await _dataStore.SaveAsync(updatedSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
        Assert.Equal("updated.bsky.social", result.UserName);
        Assert.True(result.HasAppPassword);

        var count = await _context.UserPlatformBlueskySettings
            .CountAsync(s => s.CreatedByEntraOid == ownerOid);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenOwnerMatches()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateBlueskySettingsAsync(ownerOid);

        var entity = await _context.UserPlatformBlueskySettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);
        var settingsId = entity.Id;

        // Act
        var result = await _dataStore.DeleteAsync(settingsId, ownerOid);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.UserPlatformBlueskySettings.FindAsync(settingsId));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenOwnerMismatch()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateBlueskySettingsAsync(ownerAOid);

        var entity = await _context.UserPlatformBlueskySettings.FirstAsync(s => s.CreatedByEntraOid == ownerAOid);
        var settingsId = entity.Id;

        // Act — user B attempts to delete user A's settings (MUST FAIL)
        var result = await _dataStore.DeleteAsync(settingsId, ownerBOid);

        // Assert
        Assert.False(result);
        var stillExists = await _context.UserPlatformBlueskySettings.FindAsync(settingsId);
        Assert.NotNull(stillExists);
        Assert.Equal(ownerAOid, stillExists.CreatedByEntraOid);
    }
}


