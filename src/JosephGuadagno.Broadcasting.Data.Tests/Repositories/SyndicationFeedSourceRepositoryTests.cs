using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests.Repositories;

public class SyndicationFeedSourceRepositoryTests
{
    private readonly Mock<ISyndicationFeedSourceDataStore> _dataStoreMock;
    private readonly SyndicationFeedSourceRepository _repository;

    public SyndicationFeedSourceRepositoryTests()
    {
        _dataStoreMock = new Mock<ISyndicationFeedSourceDataStore>();
        _repository = new SyndicationFeedSourceRepository(_dataStoreMock.Object);
    }

    private static SyndicationFeedSource CreateSource(int id = 1) =>
        new SyndicationFeedSource
        {
            Id = id,
            FeedIdentifier = $"feed{id}",
            Author = "Author",
            Title = "Title",
            Url = $"https://example.com/feed{id}",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task GetAsync_WithPrimaryKey_DelegatesToDataStore()
    {
        // Arrange
        var source = CreateSource();
        _dataStoreMock.Setup(d => d.GetAsync(1)).ReturnsAsync(source);

        // Act
        var result = await _repository.GetAsync(1);

        // Assert
        Assert.Equal(source, result);
        _dataStoreMock.Verify(d => d.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var source = CreateSource();
        _dataStoreMock.Setup(d => d.SaveAsync(source)).ReturnsAsync(source);

        // Act
        var result = await _repository.SaveAsync(source);

        // Assert
        Assert.Equal(source, result);
        _dataStoreMock.Verify(d => d.SaveAsync(source), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToDataStore()
    {
        // Arrange
        var sources = new List<SyndicationFeedSource> { CreateSource(1), CreateSource(2) };
        _dataStoreMock.Setup(d => d.GetAllAsync()).ReturnsAsync(sources);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(sources, result);
        _dataStoreMock.Verify(d => d.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var source = CreateSource();
        _dataStoreMock.Setup(d => d.DeleteAsync(source)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(source);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(source), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithPrimaryKey_DelegatesToDataStore()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByUrlAsync_DelegatesToDataStore()
    {
        // Arrange
        var source = CreateSource();
        _dataStoreMock.Setup(d => d.GetByUrlAsync("https://example.com/feed1")).ReturnsAsync(source);

        // Act
        var result = await _repository.GetByUrlAsync("https://example.com/feed1");

        // Assert
        Assert.Equal(source, result);
        _dataStoreMock.Verify(d => d.GetByUrlAsync("https://example.com/feed1"), Times.Once);
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_DelegatesToDataStore()
    {
        // Arrange
        var source = CreateSource();
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-7);
        var excludedCategories = new List<string> { "category1" };
        _dataStoreMock.Setup(d => d.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories)).ReturnsAsync(source);

        // Act
        var result = await _repository.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);

        // Assert
        Assert.Equal(source, result);
        _dataStoreMock.Verify(d => d.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories), Times.Once);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_DelegatesToDataStore()
    {
        // Arrange
        var source = CreateSource();
        _dataStoreMock.Setup(d => d.GetByFeedIdentifierAsync("feed1")).ReturnsAsync(source);

        // Act
        var result = await _repository.GetByFeedIdentifierAsync("feed1");

        // Assert
        Assert.Equal(source, result);
        _dataStoreMock.Verify(d => d.GetByFeedIdentifierAsync("feed1"), Times.Once);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.GetByFeedIdentifierAsync("missing")).ReturnsAsync((SyndicationFeedSource?)null);

        // Act
        var result = await _repository.GetByFeedIdentifierAsync("missing");

        // Assert
        Assert.Null(result);
        _dataStoreMock.Verify(d => d.GetByFeedIdentifierAsync("missing"), Times.Once);
    }
}
