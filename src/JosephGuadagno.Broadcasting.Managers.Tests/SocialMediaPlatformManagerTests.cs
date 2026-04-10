using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class SocialMediaPlatformManagerTests
{
    private readonly Mock<ISocialMediaPlatformDataStore> _dataStoreMock;
    private readonly SocialMediaPlatformManager _manager;

    public SocialMediaPlatformManagerTests()
    {
        _dataStoreMock = new Mock<ISocialMediaPlatformDataStore>();
        _manager = new SocialMediaPlatformManager(_dataStoreMock.Object);
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ShouldCallDataStoreWithIncludeInactiveFalse()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new() { Id = 1, Name = "Twitter", Url = "https://twitter.com", IsActive = true },
            new() { Id = 2, Name = "BlueSky", Url = "https://bsky.app", IsActive = true }
        };
        _dataStoreMock.Setup(d => d.GetAllAsync(false, default)).ReturnsAsync(platforms);

        // Act
        var result = await _manager.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(platforms);
        _dataStoreMock.Verify(d => d.GetAllAsync(false, default), Times.Once);
        _dataStoreMock.Verify(d => d.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.GetAllAsync(false, default))
            .ReturnsAsync(new List<SocialMediaPlatform>());

        // Act
        var result = await _manager.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _dataStoreMock.Verify(d => d.GetAllAsync(false, default), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenFound_ShouldReturnPlatform()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 1, Name = "Twitter", Url = "https://twitter.com", IsActive = true };
        _dataStoreMock.Setup(d => d.GetAsync(1, default)).ReturnsAsync(platform);

        // Act
        var result = await _manager.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(platform);
        _dataStoreMock.Verify(d => d.GetAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.GetAsync(99, default)).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _manager.GetByIdAsync(99);

        // Assert
        result.Should().BeNull();
        _dataStoreMock.Verify(d => d.GetAsync(99, default), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetByNameAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByNameAsync_WhenFound_ShouldReturnPlatform()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 3, Name = "Mastodon", Url = "https://mastodon.social", IsActive = true };
        _dataStoreMock.Setup(d => d.GetByNameAsync("Mastodon", default)).ReturnsAsync(platform);

        // Act
        var result = await _manager.GetByNameAsync("Mastodon");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(platform);
        _dataStoreMock.Verify(d => d.GetByNameAsync("Mastodon", default), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.GetByNameAsync("NonExistent", default)).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _manager.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
        _dataStoreMock.Verify(d => d.GetByNameAsync("NonExistent", default), Times.Once);
    }

    // -------------------------------------------------------------------------
    // AddAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddAsync_ShouldDelegateToDataStore()
    {
        // Arrange
        var request = new SocialMediaPlatform { Name = "LinkedIn", Url = "https://linkedin.com", IsActive = true };
        var saved = new SocialMediaPlatform { Id = 5, Name = "LinkedIn", Url = "https://linkedin.com", IsActive = true };
        _dataStoreMock.Setup(d => d.AddAsync(request, default)).ReturnsAsync(saved);

        // Act
        var result = await _manager.AddAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result!.Name.Should().Be("LinkedIn");
        _dataStoreMock.Verify(d => d.AddAsync(request, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenDataStoreFails_ShouldReturnNull()
    {
        // Arrange
        var request = new SocialMediaPlatform { Name = "Facebook", IsActive = true };
        _dataStoreMock.Setup(d => d.AddAsync(request, default)).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _manager.AddAsync(request);

        // Assert
        result.Should().BeNull();
        _dataStoreMock.Verify(d => d.AddAsync(request, default), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldDelegateToDataStore()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 2, Name = "Mastodon", Url = "https://mastodon.social", IsActive = true };
        _dataStoreMock.Setup(d => d.UpdateAsync(platform, default)).ReturnsAsync(platform);

        // Act
        var result = await _manager.UpdateAsync(platform);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(platform);
        _dataStoreMock.Verify(d => d.UpdateAsync(platform, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 999, Name = "Ghost Platform", IsActive = true };
        _dataStoreMock.Setup(d => d.UpdateAsync(platform, default)).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _manager.UpdateAsync(platform);

        // Assert
        result.Should().BeNull();
        _dataStoreMock.Verify(d => d.UpdateAsync(platform, default), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenFound_ShouldReturnTrue()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.DeleteAsync(3, default)).ReturnsAsync(true);

        // Act
        var result = await _manager.DeleteAsync(3);

        // Assert
        result.Should().BeTrue();
        _dataStoreMock.Verify(d => d.DeleteAsync(3, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.DeleteAsync(999, default)).ReturnsAsync(false);

        // Act
        var result = await _manager.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
        _dataStoreMock.Verify(d => d.DeleteAsync(999, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldToggleIsActive_ByDelegatingToDataStore()
    {
        // Arrange
        // The manager delegates the soft-delete (IsActive = false) responsibility to the
        // data store. Verify the data store is called with the correct ID and reports success.
        const int platformId = 7;
        var beforeDelete = new SocialMediaPlatform { Id = platformId, Name = "Pinterest", IsActive = true };

        _dataStoreMock.Setup(d => d.DeleteAsync(platformId, default))
            .Callback(() => beforeDelete.IsActive = false)   // simulate soft-delete side-effect
            .ReturnsAsync(true);

        // Act
        var deleted = await _manager.DeleteAsync(platformId);

        // Assert
        deleted.Should().BeTrue();
        beforeDelete.IsActive.Should().BeFalse("the data store performs a soft delete by setting IsActive to false");
        _dataStoreMock.Verify(d => d.DeleteAsync(platformId, default), Times.Once);
    }
}
