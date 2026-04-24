using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserCollectorFeedSourceDataStore - isolation and security enforcement
/// </summary>
public class UserCollectorFeedSourceDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserCollectorFeedSourceDataStore>> _logger = new();
    private readonly UserCollectorFeedSourceDataStore _dataStore;

    public UserCollectorFeedSourceDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserCollectorMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserCollectorFeedSourceDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateFeedSourceAsync(
        string ownerOid,
        string feedUrl,
        string displayName = "Test Feed",
        bool isActive = true)
    {
        _context.UserCollectorFeedSources.Add(new UserCollectorFeedSource
        {
            CreatedByEntraOid = ownerOid,
            FeedUrl = feedUrl,
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

        await CreateFeedSourceAsync(userAOid, "https://usera.com/feed.xml", "User A Feed 1");
        await CreateFeedSourceAsync(userAOid, "https://usera.com/feed2.xml", "User A Feed 2");
        await CreateFeedSourceAsync(userBOid, "https://userb.com/feed.xml", "User B Feed");

        // Act
        var resultA = await _dataStore.GetByUserAsync(userAOid);
        var resultB = await _dataStore.GetByUserAsync(userBOid);

        // Assert
        Assert.Equal(2, resultA.Count);
        Assert.All(resultA, config => Assert.Equal(userAOid, config.CreatedByEntraOid));
        Assert.Contains(resultA, c => c.FeedUrl == "https://usera.com/feed.xml");
        Assert.Contains(resultA, c => c.FeedUrl == "https://usera.com/feed2.xml");

        Assert.Single(resultB);
        Assert.Equal(userBOid, resultB[0].CreatedByEntraOid);
        Assert.Equal("https://userb.com/feed.xml", resultB[0].FeedUrl);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsEmptyListWhenUserHasNoConfigs()
    {
        // Arrange
        const string userAOid = "user-a-oid-11111111";
        const string userBOid = "user-b-oid-22222222";

        await CreateFeedSourceAsync(userAOid, "https://usera.com/feed.xml");

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

        await CreateFeedSourceAsync(userAOid, "https://usera.com/feed.xml", "User A Active", isActive: true);
        await CreateFeedSourceAsync(userBOid, "https://userb.com/feed.xml", "User B Active", isActive: true);
        await CreateFeedSourceAsync(userAOid, "https://usera.com/inactive.xml", "User A Inactive", isActive: false);

        // Act
        var result = await _dataStore.GetAllActiveAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, config => Assert.True(config.IsActive));
        Assert.Contains(result, c => c.CreatedByEntraOid == userAOid && c.FeedUrl == "https://usera.com/feed.xml");
        Assert.Contains(result, c => c.CreatedByEntraOid == userBOid && c.FeedUrl == "https://userb.com/feed.xml");
    }

    [Fact]
    public async Task GetAllActiveAsync_ExcludesInactiveConfigs()
    {
        // Arrange
        const string userAOid = "user-a-oid-11111111";
        const string userBOid = "user-b-oid-22222222";

        await CreateFeedSourceAsync(userAOid, "https://usera.com/inactive1.xml", "Inactive 1", isActive: false);
        await CreateFeedSourceAsync(userBOid, "https://userb.com/inactive2.xml", "Inactive 2", isActive: false);

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
        var newConfig = new Domain.Models.UserCollectorFeedSource
        {
            CreatedByEntraOid = ownerOid,
            FeedUrl = "https://example.com/feed.xml",
            DisplayName = "New Feed",
            IsActive = true
        };

        // Act
        var result = await _dataStore.SaveAsync(newConfig);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("https://example.com/feed.xml", result.FeedUrl);
        Assert.Equal("New Feed", result.DisplayName);

        var persisted = await _context.UserCollectorFeedSources.FirstOrDefaultAsync(c => c.Id == result.Id);
        Assert.NotNull(persisted);
        Assert.Equal(ownerOid, persisted.CreatedByEntraOid);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingConfigByOwnerAndUrl()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        const string feedUrl = "https://example.com/feed.xml";

        await CreateFeedSourceAsync(ownerOid, feedUrl, "Original Name");

        var updatedConfig = new Domain.Models.UserCollectorFeedSource
        {
            CreatedByEntraOid = ownerOid,
            FeedUrl = feedUrl,
            DisplayName = "Updated Name",
            IsActive = false
        };

        // Act
        var result = await _dataStore.SaveAsync(updatedConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.DisplayName);
        Assert.False(result.IsActive);

        var allConfigs = await _context.UserCollectorFeedSources
            .Where(c => c.CreatedByEntraOid == ownerOid && c.FeedUrl == feedUrl)
            .ToListAsync();
        Assert.Single(allConfigs);
        Assert.Equal("Updated Name", allConfigs[0].DisplayName);
    }

    [Fact]
    public async Task DeleteAsync_DeletesOnlyWhenOwnerMatches()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateFeedSourceAsync(ownerOid, "https://example.com/feed.xml");

        var entity = await _context.UserCollectorFeedSources
            .FirstAsync(c => c.CreatedByEntraOid == ownerOid);
        var configId = entity.Id;

        // Act - owner attempts delete (should succeed)
        var result = await _dataStore.DeleteAsync(configId, ownerOid);

        // Assert
        Assert.True(result);

        var deleted = await _context.UserCollectorFeedSources.FindAsync(configId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenIdExistsButOwnerMismatch()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";

        await CreateFeedSourceAsync(ownerAOid, "https://usera.com/feed.xml");

        var entity = await _context.UserCollectorFeedSources
            .FirstAsync(c => c.CreatedByEntraOid == ownerAOid);
        var configId = entity.Id;

        // Act - user B attempts to delete user A's config (MUST FAIL)
        var result = await _dataStore.DeleteAsync(configId, ownerBOid);

        // Assert
        Assert.False(result);

        var stillExists = await _context.UserCollectorFeedSources.FindAsync(configId);
        Assert.NotNull(stillExists);
        Assert.Equal(ownerAOid, stillExists.CreatedByEntraOid);
    }
}
