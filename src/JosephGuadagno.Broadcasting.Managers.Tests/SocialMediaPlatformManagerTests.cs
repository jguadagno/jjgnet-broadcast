using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class SocialMediaPlatformManagerTests
{
    private readonly Mock<ISocialMediaPlatformDataStore> _dataStore;
    private readonly IMemoryCache _cache;
    private readonly SocialMediaPlatformManager _sut;

    public SocialMediaPlatformManagerTests()
    {
        _dataStore = new Mock<ISocialMediaPlatformDataStore>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new SocialMediaPlatformManager(_dataStore.Object, _cache);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WhenPlatformsExist_ShouldReturnOnlyActivePlatforms()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true },
            new SocialMediaPlatform { Id = 2, Name = "BlueSky", IsActive = true }
        };
        _dataStore
            .Setup(d => d.GetAllAsync(false, default))
            .ReturnsAsync(platforms);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(platforms);
        _dataStore.Verify(d => d.GetAllAsync(false, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoActivePlatformsExist_ShouldReturnEmptyList()
    {
        // Arrange
        _dataStore
            .Setup(d => d.GetAllAsync(false, default))
            .ReturnsAsync(new List<SocialMediaPlatform>());

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _dataStore.Verify(d => d.GetAllAsync(false, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassIncludeInactiveFalseToDataStore()
    {
        // Arrange
        _dataStore
            .Setup(d => d.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialMediaPlatform>());

        // Act
        await _sut.GetAllAsync();

        // Assert — ensure inactive platforms are excluded by passing false
        _dataStore.Verify(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Once);
        _dataStore.Verify(d => d.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_OnCacheHit_ShouldReturnCachedResultWithoutCallingDataStore()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true }
        };
        _dataStore
            .Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platforms);

        // Act — first call populates the cache
        await _sut.GetAllAsync();
        // second call should be served from cache
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(platforms);
        _dataStore.Verify(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_OnCacheMiss_ShouldCallDataStoreAndPopulateCache()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true }
        };
        _dataStore
            .Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platforms);

        // Act — cache is empty, so data store must be called
        var result = await _sut.GetAllAsync();

        // Assert — data store was called and cache is now populated
        result.Should().BeEquivalentTo(platforms);
        _dataStore.Verify(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Once);
        _cache.TryGetValue("SocialMediaPlatforms_All", out List<SocialMediaPlatform>? cached);
        cached.Should().BeEquivalentTo(platforms);
    }

    // ── GetAllIncludingInactiveAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAllIncludingInactiveAsync_WhenPlatformsExist_ShouldReturnAllPlatforms()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true },
            new SocialMediaPlatform { Id = 2, Name = "MySpace", IsActive = false }
        };
        _dataStore
            .Setup(d => d.GetAllAsync(true, default))
            .ReturnsAsync(platforms);

        // Act
        var result = await _sut.GetAllIncludingInactiveAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(platforms);
        _dataStore.Verify(d => d.GetAllAsync(true, default), Times.Once);
    }

    [Fact]
    public async Task GetAllIncludingInactiveAsync_ShouldPassIncludeInactiveTrueToDataStore()
    {
        // Arrange
        _dataStore
            .Setup(d => d.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialMediaPlatform>());

        // Act
        await _sut.GetAllIncludingInactiveAsync();

        // Assert — both active and inactive platforms should be requested
        _dataStore.Verify(d => d.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _dataStore.Verify(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllIncludingInactiveAsync_OnCacheHit_ShouldNotCallDataStore()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true },
            new SocialMediaPlatform { Id = 2, Name = "MySpace", IsActive = false }
        };
        _dataStore
            .Setup(d => d.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platforms);

        // Act — first call populates the cache; second should be a cache hit
        await _sut.GetAllIncludingInactiveAsync();
        var result = await _sut.GetAllIncludingInactiveAsync();

        // Assert
        result.Should().BeEquivalentTo(platforms);
        _dataStore.Verify(d => d.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenPlatformExists_ShouldReturnPlatform()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true };
        _dataStore
            .Setup(d => d.GetAsync(1, default))
            .ReturnsAsync(platform);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Twitter");
        _dataStore.Verify(d => d.GetAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPlatformDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _dataStore
            .Setup(d => d.GetAsync(99, default))
            .ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _sut.GetByIdAsync(99);

        // Assert
        result.Should().BeNull();
        _dataStore.Verify(d => d.GetAsync(99, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_OnCacheHit_ShouldReturnCachedResultWithoutCallingDataStore()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 5, Name = "LinkedIn", IsActive = true };
        _dataStore
            .Setup(d => d.GetAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        // Act — first call populates the cache; second should be a cache hit
        await _sut.GetByIdAsync(5);
        var result = await _sut.GetByIdAsync(5);

        // Assert
        result.Should().BeEquivalentTo(platform);
        _dataStore.Verify(d => d.GetAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_OnCacheMiss_ShouldCallDataStoreAndPopulateCache()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 7, Name = "Mastodon", IsActive = true };
        _dataStore
            .Setup(d => d.GetAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        // Act
        var result = await _sut.GetByIdAsync(7);

        // Assert — data store was called and entry is now in cache
        result.Should().BeEquivalentTo(platform);
        _dataStore.Verify(d => d.GetAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        _cache.TryGetValue("SocialMediaPlatform_7", out SocialMediaPlatform? cached);
        cached.Should().BeEquivalentTo(platform);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPlatformDoesNotExist_ShouldNotCacheNullResult()
    {
        // Arrange
        _dataStore
            .Setup(d => d.GetAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        // Act — call twice; data store should be hit both times since null is not cached
        await _sut.GetByIdAsync(99);
        await _sut.GetByIdAsync(99);

        // Assert
        _dataStore.Verify(d => d.GetAsync(99, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ── GetByNameAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByNameAsync_WhenPlatformExists_ShouldReturnPlatform()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 3, Name = "BlueSky", IsActive = true };
        _dataStore
            .Setup(d => d.GetByNameAsync("BlueSky", default))
            .ReturnsAsync(platform);

        // Act
        var result = await _sut.GetByNameAsync("BlueSky");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("BlueSky");
        _dataStore.Verify(d => d.GetByNameAsync("BlueSky", default), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenPlatformDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _dataStore
            .Setup(d => d.GetByNameAsync("NonExistent", default))
            .ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _sut.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
        _dataStore.Verify(d => d.GetByNameAsync("NonExistent", default), Times.Once);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_WhenPlatformIsValid_ShouldReturnCreatedPlatform()
    {
        // Arrange
        var input = new SocialMediaPlatform { Name = "Mastodon", Url = "https://mastodon.social", IsActive = true };
        var created = new SocialMediaPlatform { Id = 10, Name = "Mastodon", Url = "https://mastodon.social", IsActive = true };
        _dataStore
            .Setup(d => d.AddAsync(input, default))
            .ReturnsAsync(created);

        // Act
        var result = await _sut.AddAsync(input);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        result.Name.Should().Be("Mastodon");
        _dataStore.Verify(d => d.AddAsync(input, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenDataStoreFails_ShouldReturnNull()
    {
        // Arrange
        var input = new SocialMediaPlatform { Name = "Mastodon" };
        _dataStore
            .Setup(d => d.AddAsync(It.IsAny<SocialMediaPlatform>(), default))
            .ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _sut.AddAsync(input);

        // Assert
        result.Should().BeNull();
        _dataStore.Verify(d => d.AddAsync(input, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldForwardCancellationTokenToDataStore()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Name = "LinkedIn" };
        var cts = new CancellationTokenSource();
        _dataStore
            .Setup(d => d.AddAsync(platform, cts.Token))
            .ReturnsAsync(platform);

        // Act
        await _sut.AddAsync(platform, cts.Token);

        // Assert
        _dataStore.Verify(d => d.AddAsync(platform, cts.Token), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldInvalidateListCaches()
    {
        // Arrange — pre-populate the list cache
        var existing = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true }
        };
        _dataStore
            .Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        await _sut.GetAllAsync(); // populates cache

        var input = new SocialMediaPlatform { Name = "Mastodon" };
        _dataStore
            .Setup(d => d.AddAsync(input, default))
            .ReturnsAsync(new SocialMediaPlatform { Id = 10, Name = "Mastodon" });

        // Act
        await _sut.AddAsync(input);

        // Assert — cache is invalidated; next GetAllAsync hits data store again
        _dataStore.Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialMediaPlatform>());
        await _sut.GetAllAsync();
        _dataStore.Verify(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenPlatformExists_ShouldReturnUpdatedPlatform()
    {
        // Arrange
        var input = new SocialMediaPlatform { Id = 5, Name = "Twitter", Url = "https://x.com", IsActive = true };
        var updated = new SocialMediaPlatform { Id = 5, Name = "Twitter", Url = "https://x.com", IsActive = true };
        _dataStore
            .Setup(d => d.UpdateAsync(input, default))
            .ReturnsAsync(updated);

        // Act
        var result = await _sut.UpdateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Url.Should().Be("https://x.com");
        _dataStore.Verify(d => d.UpdateAsync(input, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenPlatformDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var input = new SocialMediaPlatform { Id = 999, Name = "Ghost" };
        _dataStore
            .Setup(d => d.UpdateAsync(It.IsAny<SocialMediaPlatform>(), default))
            .ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _sut.UpdateAsync(input);

        // Assert
        result.Should().BeNull();
        _dataStore.Verify(d => d.UpdateAsync(input, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldForwardCancellationTokenToDataStore()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 1, Name = "LinkedIn" };
        var cts = new CancellationTokenSource();
        _dataStore
            .Setup(d => d.UpdateAsync(platform, cts.Token))
            .ReturnsAsync(platform);

        // Act
        await _sut.UpdateAsync(platform, cts.Token);

        // Assert
        _dataStore.Verify(d => d.UpdateAsync(platform, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldInvalidateListCachesAndByIdCache()
    {
        // Arrange — pre-populate list and by-id caches
        var platformList = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 3, Name = "BlueSky", IsActive = true }
        };
        var platform = new SocialMediaPlatform { Id = 3, Name = "BlueSky", IsActive = true };
        _dataStore.Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(platformList);
        _dataStore.Setup(d => d.GetAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(platform);
        await _sut.GetAllAsync();
        await _sut.GetByIdAsync(3);

        var updated = new SocialMediaPlatform { Id = 3, Name = "BlueSky Updated" };
        _dataStore.Setup(d => d.UpdateAsync(It.IsAny<SocialMediaPlatform>(), default)).ReturnsAsync(updated);

        // Act
        await _sut.UpdateAsync(updated);

        // Assert — subsequent reads hit data store again (caches cleared)
        _dataStore.Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(new List<SocialMediaPlatform>());
        _dataStore.Setup(d => d.GetAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(updated);
        await _sut.GetAllAsync();
        await _sut.GetByIdAsync(3);
        _dataStore.Verify(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _dataStore.Verify(d => d.GetAsync(3, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenPlatformExists_ShouldReturnTrue()
    {
        // Arrange
        _dataStore
            .Setup(d => d.DeleteAsync(7, default))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(7);

        // Assert
        result.Should().BeTrue();
        _dataStore.Verify(d => d.DeleteAsync(7, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenPlatformDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _dataStore
            .Setup(d => d.DeleteAsync(999, default))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
        _dataStore.Verify(d => d.DeleteAsync(999, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldForwardCancellationTokenToDataStore()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _dataStore
            .Setup(d => d.DeleteAsync(1, cts.Token))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(1, cts.Token);

        // Assert
        _dataStore.Verify(d => d.DeleteAsync(1, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldInvalidateListCachesAndByIdCache()
    {
        // Arrange — pre-populate list and by-id caches
        var platformList = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 4, Name = "Threads", IsActive = true }
        };
        var platform = new SocialMediaPlatform { Id = 4, Name = "Threads", IsActive = true };
        _dataStore.Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(platformList);
        _dataStore.Setup(d => d.GetAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(platform);
        await _sut.GetAllAsync();
        await _sut.GetByIdAsync(4);

        _dataStore.Setup(d => d.DeleteAsync(4, default)).ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(4);

        // Assert — subsequent reads hit data store again (caches cleared)
        _dataStore.Setup(d => d.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(new List<SocialMediaPlatform>());
        _dataStore.Setup(d => d.GetAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync((SocialMediaPlatform?)null);
        await _sut.GetAllAsync();
        await _sut.GetByIdAsync(4);
        _dataStore.Verify(d => d.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _dataStore.Verify(d => d.GetAsync(4, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
