using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserPublisherTwitterSettingsDataStore — isolation and owner enforcement
/// </summary>
public class UserPublisherTwitterSettingsDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserPublisherTwitterSettingsDataStore>> _logger = new();
    private readonly UserPublisherTwitterSettingsDataStore _dataStore;

    public UserPublisherTwitterSettingsDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserPublisherSettingsMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserPublisherTwitterSettingsDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateTwitterSettingsAsync(
        string ownerOid,
        bool isEnabled = true,
        bool hasConsumerKey = false,
        bool hasAccessToken = false)
    {
        _context.UserPublisherTwitterSettings.Add(new UserPublisherTwitterSettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = isEnabled,
            HasConsumerKey = hasConsumerKey,
            HasConsumerSecret = false,
            HasAccessToken = hasAccessToken,
            HasAccessTokenSecret = false,
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
        await CreateTwitterSettingsAsync(ownerOid, isEnabled: true, hasConsumerKey: true);

        // Act
        var result = await _dataStore.GetByUserAsync(ownerOid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.True(result.HasConsumerKey);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsNullWhenUserHasNoSettings()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateTwitterSettingsAsync(ownerAOid);

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
        await CreateTwitterSettingsAsync(ownerOid);

        var entity = await _context.UserPublisherTwitterSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);

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
        var newSettings = new Domain.Models.UserPublisherTwitterSettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            HasConsumerKey = true,
            HasConsumerSecret = true,
            HasAccessToken = true,
            HasAccessTokenSecret = true
        };

        // Act
        var result = await _dataStore.SaveAsync(newSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.True(result.HasConsumerKey);
        Assert.True(result.HasAccessToken);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSettingsForSameOwner()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateTwitterSettingsAsync(ownerOid, isEnabled: false);

        var updatedSettings = new Domain.Models.UserPublisherTwitterSettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            HasConsumerKey = true,
            HasConsumerSecret = true,
            HasAccessToken = true,
            HasAccessTokenSecret = true
        };

        // Act
        var result = await _dataStore.SaveAsync(updatedSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
        Assert.True(result.HasConsumerKey);

        var count = await _context.UserPublisherTwitterSettings
            .CountAsync(s => s.CreatedByEntraOid == ownerOid);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenOwnerMatches()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateTwitterSettingsAsync(ownerOid);

        var entity = await _context.UserPublisherTwitterSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);
        var settingsId = entity.Id;

        // Act
        var result = await _dataStore.DeleteAsync(settingsId, ownerOid);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.UserPublisherTwitterSettings.FindAsync(settingsId));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenOwnerMismatch()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateTwitterSettingsAsync(ownerAOid);

        var entity = await _context.UserPublisherTwitterSettings.FirstAsync(s => s.CreatedByEntraOid == ownerAOid);
        var settingsId = entity.Id;

        // Act — user B attempts to delete user A's settings (MUST FAIL)
        var result = await _dataStore.DeleteAsync(settingsId, ownerBOid);

        // Assert
        Assert.False(result);
        var stillExists = await _context.UserPublisherTwitterSettings.FindAsync(settingsId);
        Assert.NotNull(stillExists);
        Assert.Equal(ownerAOid, stillExists.CreatedByEntraOid);
    }
}
