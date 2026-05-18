using FluentAssertions;
using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class YouTubeItemManagerTests
{
    private readonly Mock<IYouTubeItemDataStore> _repository;
    private readonly IMemoryCache _cache;
    private readonly YouTubeItemManager _youTubeItemManager;

    public YouTubeItemManagerTests()
    {
        _repository = new Mock<IYouTubeItemDataStore>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _youTubeItemManager = new YouTubeItemManager(_repository.Object, _cache);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeItem { Id = 1, CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync(source);

        // Act
        var result = await _youTubeItemManager.GetAsync(1);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeItem { Id = 1, CreatedByEntraOid = "" };
        _repository.Setup(r => r.SaveAsync(source, default)).ReturnsAsync(OperationResult<YouTubeItem>.Success(source));

        // Act
        var result = await _youTubeItemManager.SaveAsync(source);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(source, result.Value);
        _repository.Verify(r => r.SaveAsync(source, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var sources = new List<YouTubeItem> { new YouTubeItem { Id = 1, CreatedByEntraOid = "" } };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        // Act
        var result = await _youTubeItemManager.GetAllAsync();

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithOwnerOid_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var sources = new List<YouTubeItem> { new YouTubeItem { Id = 1, CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetAllAsync("owner-1", default)).ReturnsAsync(sources);

        // Act
        var result = await _youTubeItemManager.GetAllAsync("owner-1");

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync("owner-1", default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_OnCacheHit_ShouldReturnCachedResultWithoutCallingDataStore()
    {
        // Arrange
        var sources = new List<YouTubeItem> { new YouTubeItem { Id = 1, CreatedByEntraOid = "" } };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        // Act — first call populates cache, second is served from cache
        await _youTubeItemManager.GetAllAsync();
        var result = await _youTubeItemManager.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(sources);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithOwnerOid_OnCacheHit_ShouldReturnCachedResultWithoutCallingDataStore()
    {
        // Arrange
        var sources = new List<YouTubeItem> { new YouTubeItem { Id = 1, CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        // Act — first call populates cache, second is served from cache
        await _youTubeItemManager.GetAllAsync("owner-1");
        var result = await _youTubeItemManager.GetAllAsync("owner-1");

        // Assert
        result.Should().BeEquivalentTo(sources);
        _repository.Verify(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldInvalidateCacheSoNextCallHitsDataStore()
    {
        // Arrange
        var source = new YouTubeItem { Id = 1, CreatedByEntraOid = "owner-1" };
        _repository.Setup(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<YouTubeItem> { source });
        _repository.Setup(r => r.SaveAsync(source, default)).ReturnsAsync(OperationResult<YouTubeItem>.Success(source));

        // Act — populate cache, then save (invalidates), then fetch again
        await _youTubeItemManager.GetAllAsync("owner-1");
        await _youTubeItemManager.SaveAsync(source);
        await _youTubeItemManager.GetAllAsync("owner-1");

        // Assert — data store called twice: before and after invalidation
        _repository.Verify(r => r.GetAllAsync("owner-1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeItem { Id = 1, CreatedByEntraOid = "" };
        _repository.Setup(r => r.DeleteAsync(source, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _youTubeItemManager.DeleteAsync(source);

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
        var result = await _youTubeItemManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetByUrlAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeItem { Id = 1, Url = "http://test.com", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetByUrlAsync("http://test.com")).ReturnsAsync(source);

        // Act
        var result = await _youTubeItemManager.GetByUrlAsync("http://test.com");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByUrlAsync("http://test.com"), Times.Once);
    }

    [Fact]
    public async Task GetByVideoIdAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeItem { Id = 1, VideoId = "testvideoid", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetByVideoIdAsync("testvideoid")).ReturnsAsync(source);

        // Act
        var result = await _youTubeItemManager.GetByVideoIdAsync("testvideoid");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByVideoIdAsync("testvideoid"), Times.Once);
    }

    [Fact]
    public async Task GetByVideoIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetByVideoIdAsync("missing")).ReturnsAsync((YouTubeItem?)null);

        // Act
        var result = await _youTubeItemManager.GetByVideoIdAsync("missing");

        // Assert
        Assert.Null(result);
        _repository.Verify(r => r.GetByVideoIdAsync("missing"), Times.Once);
    }

    [Fact]
    public async Task IsVideoUniqueToUser_ReturnsTrue_WhenVideoDoesNotExistForUser()
    {
        // Arrange
        _repository.Setup(r => r.IsVideoUniqueToUser("vid1", "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _youTubeItemManager.IsVideoUniqueToUser("vid1", "owner-1");

        // Assert
        result.Should().BeTrue();
        _repository.Verify(r => r.IsVideoUniqueToUser("vid1", "owner-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsVideoUniqueToUser_ReturnsFalse_WhenVideoAlreadyExistsForUser()
    {
        // Arrange
        _repository.Setup(r => r.IsVideoUniqueToUser("vid1", "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _youTubeItemManager.IsVideoUniqueToUser("vid1", "owner-1");

        // Assert
        result.Should().BeFalse();
        _repository.Verify(r => r.IsVideoUniqueToUser("vid1", "owner-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsVideoUniqueToUser_ReturnsTrue_WhenVideoExistsForDifferentUser()
    {
        // Arrange — same videoId, different owner: unique for owner-2
        _repository.Setup(r => r.IsVideoUniqueToUser("vid1", "owner-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _youTubeItemManager.IsVideoUniqueToUser("vid1", "owner-2");

        // Assert
        result.Should().BeTrue();
        _repository.Verify(r => r.IsVideoUniqueToUser("vid1", "owner-2", It.IsAny<CancellationToken>()), Times.Once);
    }
}
