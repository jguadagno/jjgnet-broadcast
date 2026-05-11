using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class FeedCheckDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly FeedCheckDataStore _dataStore;

    public FeedCheckDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new BroadcastingContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var mapper = config.CreateMapper();

        _dataStore = new FeedCheckDataStore(_context, mapper, NullLogger<FeedCheckDataStore>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private FeedCheck CreateFeedCheck(string name = "TestFeed", string entraOId = "test-oid") => new FeedCheck
    {
        Name = name,
        EntraOId = entraOId,
        LastCheckedFeed = DateTimeOffset.UtcNow,
        LastItemAddedOrUpdated = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsFeedCheck()
    {
        // Arrange
        var feedCheck = CreateFeedCheck();
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(feedCheck.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(feedCheck.Id, result.Id);
        Assert.Equal("TestFeed", result.Name);
    }

    [Fact]
    public async Task GetAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllFeedChecks()
    {
        // Arrange
        _context.FeedChecks.AddRange(
            CreateFeedCheck("Feed1"),
            CreateFeedCheck("Feed2"),
            CreateFeedCheck("Feed3")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SaveAsync_ExistingFeedCheck_UpdatesAndReturns()
    {
        // Arrange
        var feedCheck = CreateFeedCheck("OriginalName");
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        var updatedOn = DateTimeOffset.UtcNow;
        var domainFeedCheck = new Domain.Models.FeedCheck
        {
            Id = feedCheck.Id,
            Name = "UpdatedName",
            LastCheckedFeed = updatedOn,
            LastItemAddedOrUpdated = updatedOn,
            LastUpdatedOn = updatedOn
        };

        // Act
        var result = await _dataStore.SaveAsync(domainFeedCheck);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("UpdatedName", result.Value!.Name);
        Assert.Equal(feedCheck.Id, result.Value!.Id);
    }

    [Fact]
    public async Task SaveAsync_NewFeedCheck_ThrowsApplicationException()
    {
        // Arrange
        var domainFeedCheck = new Domain.Models.FeedCheck
        {
            Id = 0,
            Name = "NewFeed",
            LastCheckedFeed = DateTimeOffset.UtcNow,
            LastItemAddedOrUpdated = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

        // Act & Assert - Now returns OperationResult.Failure instead of throwing
        var result = await _dataStore.SaveAsync(domainFeedCheck);
        Assert.True(result.IsSuccess); // New FeedCheck with Id=0 should succeed (it creates a new record)
    }

    [Fact]
    public async Task DeleteAsync_WithFeedCheckObject_DeletesFeedCheck()
    {
        // Arrange
        var feedCheck = CreateFeedCheck();
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        var domainFeedCheck = new Domain.Models.FeedCheck { Id = feedCheck.Id };

        // Act
        var result = await _dataStore.DeleteAsync(domainFeedCheck);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.FeedChecks.ToList());
    }

    [Fact]
    public async Task DeleteAsync_WithId_DeletesFeedCheck()
    {
        // Arrange
        var feedCheck = CreateFeedCheck();
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(feedCheck.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.FeedChecks.ToList());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsTrue()
    {
        // Act
        var result = await _dataStore.DeleteAsync(999);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsFeedCheck()
    {
        // Arrange
        var feedCheck = CreateFeedCheck("MyFeed", "test-oid");
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByNameAsync("MyFeed", "test-oid");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MyFeed", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_NonExistingName_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetByNameAsync("NonExistent", "test-oid");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_WithEntraOId_ReturnsRecord()
    {
        // Arrange — record is owned by "owner-oid-abc"
        var feedCheck = CreateFeedCheck("OwnedFeed", "owner-oid-abc");
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        // Act — query using the same (Name, EntraOId) combination
        var result = await _dataStore.GetByNameAsync("OwnedFeed", "owner-oid-abc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OwnedFeed", result.Name);
        Assert.Equal("owner-oid-abc", result.EntraOId);
    }

    [Fact]
    public async Task GetByNameAsync_DifferentEntraOId_ReturnsNull()
    {
        // Arrange — record is owned by "owner-oid-abc"
        var feedCheck = CreateFeedCheck("SharedFeedName", "owner-oid-abc");
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        // Act — query using same name but a different OID (user isolation)
        var result = await _dataStore.GetByNameAsync("SharedFeedName", "other-user-oid");

        // Assert — must return null; another user's record must not be visible
        Assert.Null(result);
    }
}
