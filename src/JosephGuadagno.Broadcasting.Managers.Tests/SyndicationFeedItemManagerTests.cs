using FluentAssertions;
using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class SyndicationFeedItemManagerTests
{
    private readonly Mock<ISyndicationFeedItemDataStore> _repository;
    private readonly IMemoryCache _cache;
    private readonly SyndicationFeedItemManager _syndicationFeedItemManager;

    public SyndicationFeedItemManagerTests()
    {
        _repository = new Mock<ISyndicationFeedItemDataStore>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _syndicationFeedItemManager = new SyndicationFeedItemManager(_repository.Object, _cache);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetAsync(1, default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedItemManager.GetAsync(1);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" };
        _repository.Setup(r => r.SaveAsync(source, default)).ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(source));

        // Act
        var result = await _syndicationFeedItemManager.SaveAsync(source);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(source, result.Value);
        _repository.Verify(r => r.SaveAsync(source, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var sources = new List<SyndicationFeedItem> { new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" } };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        // Act
        var result = await _syndicationFeedItemManager.GetAllAsync();

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithOwnerOid_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var sources = new List<SyndicationFeedItem> { new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetAllAsync("owner-1", default)).ReturnsAsync(sources);

        // Act
        var result = await _syndicationFeedItemManager.GetAllAsync("owner-1");

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync("owner-1", default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_OnCacheHit_ShouldReturnCachedResultWithoutCallingDataStore()
    {
        // Arrange
        var sources = new List<SyndicationFeedItem> { new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" } };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        // Act — first call populates cache, second is served from cache
        await _syndicationFeedItemManager.GetAllAsync();
        var result = await _syndicationFeedItemManager.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(sources);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithOwnerOid_OnCacheHit_ShouldReturnCachedResultWithoutCallingDataStore()
    {
        // Arrange
        var sources = new List<SyndicationFeedItem> { new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        // Act — first call populates cache, second is served from cache
        await _syndicationFeedItemManager.GetAllAsync("owner-1");
        var result = await _syndicationFeedItemManager.GetAllAsync("owner-1");

        // Assert
        result.Should().BeEquivalentTo(sources);
        _repository.Verify(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldInvalidateCacheSoNextCallHitsDataStore()
    {
        // Arrange
        var source = new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "owner-1" };
        _repository.Setup(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<SyndicationFeedItem> { source });
        _repository.Setup(r => r.SaveAsync(source, default)).ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(source));

        // Act — populate cache, then save (invalidates), then fetch again
        await _syndicationFeedItemManager.GetAllAsync("owner-1");
        await _syndicationFeedItemManager.SaveAsync(source);
        await _syndicationFeedItemManager.GetAllAsync("owner-1");

        // Assert — data store called twice: before and after invalidation
        _repository.Verify(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" };
        _repository.Setup(r => r.DeleteAsync(source, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _syndicationFeedItemManager.DeleteAsync(source);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(source, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _syndicationFeedItemManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetCollectorOwnerOidAsync_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.GetCollectorOwnerOidAsync(default)).ReturnsAsync("owner-1");

        // Act
        var result = await _syndicationFeedItemManager.GetCollectorOwnerOidAsync();

        // Assert
        Assert.Equal("owner-1", result);
        _repository.Verify(r => r.GetCollectorOwnerOidAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "owner-1" };
        var cutoffDate = DateTimeOffset.UtcNow;
        var excludedCategories = new List<string> { "Exclude" };
        _repository.Setup(r => r.GetCollectorOwnerOidAsync(default)).ReturnsAsync("owner-1");
        _repository.Setup(r => r.GetRandomSyndicationDataAsync("owner-1", cutoffDate, excludedCategories, default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedItemManager.GetRandomSyndicationDataAsync("owner-1", cutoffDate, excludedCategories);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetRandomSyndicationDataAsync("owner-1", cutoffDate, excludedCategories, default), Times.Once);
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_WithOwnerOid_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var source = new SyndicationFeedItem { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "owner-1" };
        var cutoffDate = DateTimeOffset.UtcNow;
        var excludedCategories = new List<string> { "Exclude" };
        _repository.Setup(r => r.GetRandomSyndicationDataAsync("owner-1", cutoffDate, excludedCategories, default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedItemManager.GetRandomSyndicationDataAsync("owner-1", cutoffDate, excludedCategories);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetRandomSyndicationDataAsync("owner-1", cutoffDate, excludedCategories, default), Times.Once);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedItem { Id = 1, FeedIdentifier = "test-feed-id", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetByFeedIdentifierAsync("test-feed-id", default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedItemManager.GetByFeedIdentifierAsync("test-feed-id");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByFeedIdentifierAsync("test-feed-id", default), Times.Once);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetByFeedIdentifierAsync("missing", default)).ReturnsAsync((SyndicationFeedItem?)null);

        // Act
        var result = await _syndicationFeedItemManager.GetByFeedIdentifierAsync("missing");

        // Assert
        Assert.Null(result);
        _repository.Verify(r => r.GetByFeedIdentifierAsync("missing", default), Times.Once);
    }

    [Fact]
    public async Task IsFeedItemUniqueToUser_ReturnsTrue_WhenItemDoesNotExistForUser()
    {
        // Arrange
        _repository.Setup(r => r.IsFeedItemUniqueToUser("feed-123", "user-oid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _syndicationFeedItemManager.IsFeedItemUniqueToUser("feed-123", "user-oid-1");

        // Assert
        result.Should().BeTrue();
        _repository.Verify(r => r.IsFeedItemUniqueToUser("feed-123", "user-oid-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsFeedItemUniqueToUser_ReturnsFalse_WhenItemAlreadyExistsForUser()
    {
        // Arrange
        _repository.Setup(r => r.IsFeedItemUniqueToUser("feed-123", "user-oid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _syndicationFeedItemManager.IsFeedItemUniqueToUser("feed-123", "user-oid-1");

        // Assert
        result.Should().BeFalse();
        _repository.Verify(r => r.IsFeedItemUniqueToUser("feed-123", "user-oid-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsFeedItemUniqueToUser_ReturnsTrue_WhenItemExistsForDifferentUser()
    {
        // Arrange
        _repository.Setup(r => r.IsFeedItemUniqueToUser("feed-123", "user-oid-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _syndicationFeedItemManager.IsFeedItemUniqueToUser("feed-123", "user-oid-2");

        // Assert
        result.Should().BeTrue();
        _repository.Verify(r => r.IsFeedItemUniqueToUser("feed-123", "user-oid-2", It.IsAny<CancellationToken>()), Times.Once);
    }
}
