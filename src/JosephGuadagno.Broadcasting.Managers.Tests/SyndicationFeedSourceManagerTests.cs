using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class SyndicationFeedSourceManagerTests
{
    private readonly Mock<ISyndicationFeedSourceDataStore> _repository;
    private readonly SyndicationFeedSourceManager _syndicationFeedSourceManager;

    public SyndicationFeedSourceManagerTests()
    {
        _repository = new Mock<ISyndicationFeedSourceDataStore>();
        _syndicationFeedSourceManager = new SyndicationFeedSourceManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetAsync(1, default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.GetAsync(1);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" };
        _repository.Setup(r => r.SaveAsync(source, default)).ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(source));

        // Act
        var result = await _syndicationFeedSourceManager.SaveAsync(source);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(source, result.Value);
        _repository.Verify(r => r.SaveAsync(source, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var sources = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" } };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        // Act
        var result = await _syndicationFeedSourceManager.GetAllAsync();

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" };
        _repository.Setup(r => r.DeleteAsync(source, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _syndicationFeedSourceManager.DeleteAsync(source);

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
        var result = await _syndicationFeedSourceManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetByUrlAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, Url = "http://test.com", FeedIdentifier = "Test", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetByUrlAsync("http://test.com", default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.GetByUrlAsync("http://test.com");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByUrlAsync("http://test.com", default), Times.Once);
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "Test", CreatedByEntraOid = "" };
        var cutoffDate = DateTimeOffset.UtcNow;
        var excludedCategories = new List<string> { "Exclude" };
        _repository.Setup(r => r.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories, default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories, default), Times.Once);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new SyndicationFeedSource { Id = 1, FeedIdentifier = "test-feed-id", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetByFeedIdentifierAsync("test-feed-id", default)).ReturnsAsync(source);

        // Act
        var result = await _syndicationFeedSourceManager.GetByFeedIdentifierAsync("test-feed-id");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByFeedIdentifierAsync("test-feed-id", default), Times.Once);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetByFeedIdentifierAsync("missing", default)).ReturnsAsync((SyndicationFeedSource?)null);

        // Act
        var result = await _syndicationFeedSourceManager.GetByFeedIdentifierAsync("missing");

        // Assert
        Assert.Null(result);
        _repository.Verify(r => r.GetByFeedIdentifierAsync("missing", default), Times.Once);
    }
}