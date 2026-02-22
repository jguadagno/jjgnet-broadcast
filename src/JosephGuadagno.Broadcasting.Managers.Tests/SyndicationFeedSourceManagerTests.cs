using Moq;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class SyndicationFeedSourceManagerTests
{
    private readonly Mock<ISyndicationFeedSourceRepository> _repository;
    private readonly SyndicationFeedSourceManager _syndicationFeedSourceManager;

    public SyndicationFeedSourceManagerTests()
    {
        _repository = new Mock<ISyndicationFeedSourceRepository>();
        _syndicationFeedSourceManager = new SyndicationFeedSourceManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test" };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.GetAsync(1);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test" };
        _repository.Setup(r => r.SaveAsync(source)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.SaveAsync(source);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.SaveAsync(source), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var sources = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test" } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(sources);

        // Act
        var result = await _syndicationFeedSourceManager.GetAllAsync();

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test" };
        _repository.Setup(r => r.DeleteAsync(source)).ReturnsAsync(true);

        // Act
        var result = await _syndicationFeedSourceManager.DeleteAsync(source);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(source), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _syndicationFeedSourceManager.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByUrlAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, Url = "http://test.com", FeedIdentifier = "Test" };
        _repository.Setup(r => r.GetByUrlAsync("http://test.com")).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.GetByUrlAsync("http://test.com");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByUrlAsync("http://test.com"), Times.Once);
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test" };
        var cutoffDate = DateTimeOffset.UtcNow;
        var excludedCategories = new List<string> { "Exclude" };
        _repository.Setup(r => r.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories), Times.Once);
    }
}