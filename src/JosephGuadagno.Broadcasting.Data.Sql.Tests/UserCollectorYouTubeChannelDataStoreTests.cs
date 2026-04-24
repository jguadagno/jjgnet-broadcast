using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserCollectorYouTubeChannelDataStore - isolation and security enforcement
/// </summary>
public class UserCollectorYouTubeChannelDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserCollectorYouTubeChannelDataStore>> _logger = new();
    private readonly UserCollectorYouTubeChannelDataStore _dataStore;

    public UserCollectorYouTubeChannelDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserCollectorMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserCollectorYouTubeChannelDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateYouTubeChannelAsync(
        string ownerOid,
        string channelId,
        string displayName = "Test Channel",
        bool isActive = true)
    {
        _context.UserCollectorYouTubeChannels.Add(new UserCollectorYouTubeChannel
        {
            CreatedByEntraOid = ownerOid,
            ChannelId = channelId,
            DisplayName = displayName,
            IsActive = isActive,
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyConfigsForThatUser()
    {
        // Arrange
        const string userAOid = "user-a-oid-11111111";
        const string userBOid = "user-b-oid-22222222";

        await CreateYouTubeChannelAsync(userAOid, "UC-channel-a-1", "User A Channel 1");
        await CreateYouTubeChannelAsync(userAOid, "UC-channel-a-2", "User A Channel 2");
        await CreateYouTubeChannelAsync(userBOid, "UC-channel-b-1", "User B Channel");

        // Act
        var resultA = await _dataStore.GetByUserAsync(userAOid);
        var resultB = await _dataStore.GetByUserAsync(userBOid);

        // Assert
        Assert.Equal(2, resultA.Count);
        Assert.All(resultA, config => Assert.Equal(userAOid, config.CreatedByEntraOid));
        Assert.Contains(resultA, c => c.ChannelId == "UC-channel-a-1");
        Assert.Contains(resultA, c => c.ChannelId == "UC-channel-a-2");

        Assert.Single(resultB);
        Assert.Equal(userBOid, resultB[0].CreatedByEntraOid);
        Assert.Equal("UC-channel-b-1", resultB[0].ChannelId);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsEmptyListWhenUserHasNoConfigs()
    {
        // Arrange
        const string userAOid = "user-a-oid-11111111";
        const string userBOid = "user-b-oid-22222222";

        await CreateYouTubeChannelAsync(userAOid, "UC-channel-a-1");

        // Act - query for a user with no configs
        var result = await _dataStore.GetByUserAsync(userBOid);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllActiveAsync_ReturnsAllUsersActiveConfigs()
    {
        // Arrange
        const string userAOid = "user-a-oid-11111111";
        const string userBOid = "user-b-oid-22222222";

        await CreateYouTubeChannelAsync(userAOid, "UC-channel-a-active", "User A Active", isActive: true);
        await CreateYouTubeChannelAsync(userBOid, "UC-channel-b-active", "User B Active", isActive: true);
        await CreateYouTubeChannelAsync(userAOid, "UC-channel-a-inactive", "User A Inactive", isActive: false);

        // Act
        var result = await _dataStore.GetAllActiveAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, config => Assert.True(config.IsActive));
        Assert.Contains(result, c => c.CreatedByEntraOid == userAOid && c.ChannelId == "UC-channel-a-active");
        Assert.Contains(result, c => c.CreatedByEntraOid == userBOid && c.ChannelId == "UC-channel-b-active");
    }

    [Fact]
    public async Task GetAllActiveAsync_ExcludesInactiveConfigs()
    {
        // Arrange
        const string userAOid = "user-a-oid-11111111";
        const string userBOid = "user-b-oid-22222222";

        await CreateYouTubeChannelAsync(userAOid, "UC-channel-a-inactive", "Inactive 1", isActive: false);
        await CreateYouTubeChannelAsync(userBOid, "UC-channel-b-inactive", "Inactive 2", isActive: false);

        // Act
        var result = await _dataStore.GetAllActiveAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewConfig()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        var newConfig = new Domain.Models.UserCollectorYouTubeChannel
        {
            CreatedByEntraOid = ownerOid,
            ChannelId = "UC-new-channel-123",
            DisplayName = "New Channel",
            IsActive = true
        };

        // Act
        var result = await _dataStore.SaveAsync(newConfig);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("UC-new-channel-123", result.ChannelId);
        Assert.Equal("New Channel", result.DisplayName);

        var persisted = await _context.UserCollectorYouTubeChannels.FirstOrDefaultAsync(c => c.Id == result.Id);
        Assert.NotNull(persisted);
        Assert.Equal(ownerOid, persisted.CreatedByEntraOid);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingConfigByOwnerAndChannel()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        const string channelId = "UC-existing-channel";

        await CreateYouTubeChannelAsync(ownerOid, channelId, "Original Name");

        var updatedConfig = new Domain.Models.UserCollectorYouTubeChannel
        {
            CreatedByEntraOid = ownerOid,
            ChannelId = channelId,
            DisplayName = "Updated Name",
            IsActive = false
        };

        // Act
        var result = await _dataStore.SaveAsync(updatedConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.DisplayName);
        Assert.False(result.IsActive);

        var allConfigs = await _context.UserCollectorYouTubeChannels
            .Where(c => c.CreatedByEntraOid == ownerOid && c.ChannelId == channelId)
            .ToListAsync();
        Assert.Single(allConfigs);
        Assert.Equal("Updated Name", allConfigs[0].DisplayName);
    }

    [Fact]
    public async Task DeleteAsync_DeletesOnlyWhenOwnerMatches()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateYouTubeChannelAsync(ownerOid, "UC-test-channel");

        var entity = await _context.UserCollectorYouTubeChannels
            .FirstAsync(c => c.CreatedByEntraOid == ownerOid);
        var configId = entity.Id;

        // Act - owner attempts delete (should succeed)
        var result = await _dataStore.DeleteAsync(configId, ownerOid);

        // Assert
        Assert.True(result);

        var deleted = await _context.UserCollectorYouTubeChannels.FindAsync(configId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenIdExistsButOwnerMismatch()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";

        await CreateYouTubeChannelAsync(ownerAOid, "UC-channel-a");

        var entity = await _context.UserCollectorYouTubeChannels
            .FirstAsync(c => c.CreatedByEntraOid == ownerAOid);
        var configId = entity.Id;

        // Act - user B attempts to delete user A's config (MUST FAIL)
        var result = await _dataStore.DeleteAsync(configId, ownerBOid);

        // Assert
        Assert.False(result);

        var stillExists = await _context.UserCollectorYouTubeChannels.FindAsync(configId);
        Assert.NotNull(stillExists);
        Assert.Equal(ownerAOid, stillExists.CreatedByEntraOid);
    }
}
