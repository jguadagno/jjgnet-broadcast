using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests.Repositories;

public class FeedCheckRepositoryTests
{
    private readonly Mock<IFeedCheckDataStore> _dataStoreMock;
    private readonly FeedCheckRepository _repository;

    public FeedCheckRepositoryTests()
    {
        _dataStoreMock = new Mock<IFeedCheckDataStore>();
        _repository = new FeedCheckRepository(_dataStoreMock.Object);
    }

    [Fact]
    public async Task GetAsync_WithPrimaryKey_DelegatesToDataStore()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1, Name = "Feed1", LastCheckedFeed = DateTimeOffset.UtcNow, LastItemAddedOrUpdated = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow };
        _dataStoreMock.Setup(d => d.GetAsync(1)).ReturnsAsync(feedCheck);

        // Act
        var result = await _repository.GetAsync(1);

        // Assert
        Assert.Equal(feedCheck, result);
        _dataStoreMock.Verify(d => d.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1, Name = "Feed1", LastCheckedFeed = DateTimeOffset.UtcNow, LastItemAddedOrUpdated = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow };
        _dataStoreMock.Setup(d => d.SaveAsync(feedCheck)).ReturnsAsync(feedCheck);

        // Act
        var result = await _repository.SaveAsync(feedCheck);

        // Assert
        Assert.Equal(feedCheck, result);
        _dataStoreMock.Verify(d => d.SaveAsync(feedCheck), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToDataStore()
    {
        // Arrange
        var feedChecks = new List<FeedCheck>
        {
            new FeedCheck { Id = 1, Name = "Feed1", LastCheckedFeed = DateTimeOffset.UtcNow, LastItemAddedOrUpdated = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow },
            new FeedCheck { Id = 2, Name = "Feed2", LastCheckedFeed = DateTimeOffset.UtcNow, LastItemAddedOrUpdated = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow }
        };
        _dataStoreMock.Setup(d => d.GetAllAsync()).ReturnsAsync(feedChecks);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(feedChecks, result);
        _dataStoreMock.Verify(d => d.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1, Name = "Feed1", LastCheckedFeed = DateTimeOffset.UtcNow, LastItemAddedOrUpdated = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow };
        _dataStoreMock.Setup(d => d.DeleteAsync(feedCheck)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(feedCheck);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(feedCheck), Times.Once);
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
    public async Task GetByNameAsync_DelegatesToDataStore()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1, Name = "Feed1", LastCheckedFeed = DateTimeOffset.UtcNow, LastItemAddedOrUpdated = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow };
        _dataStoreMock.Setup(d => d.GetByNameAsync("Feed1")).ReturnsAsync(feedCheck);

        // Act
        var result = await _repository.GetByNameAsync("Feed1");

        // Assert
        Assert.Equal(feedCheck, result);
        _dataStoreMock.Verify(d => d.GetByNameAsync("Feed1"), Times.Once);
    }
}
