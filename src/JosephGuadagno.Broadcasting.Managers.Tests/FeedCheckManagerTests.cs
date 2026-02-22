using Moq;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class FeedCheckManagerTests
{
    private readonly Mock<IFeedCheckRepository> _repository;
    private readonly FeedCheckManager _feedCheckManager;

    public FeedCheckManagerTests()
    {
        _repository = new Mock<IFeedCheckRepository>();
        _feedCheckManager = new FeedCheckManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1 };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync(feedCheck);

        // Act
        var result = await _feedCheckManager.GetAsync(1);

        // Assert
        Assert.Equal(feedCheck, result);
        _repository.Verify(r => r.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1 };
        _repository.Setup(r => r.SaveAsync(feedCheck)).ReturnsAsync(feedCheck);

        // Act
        var result = await _feedCheckManager.SaveAsync(feedCheck);

        // Assert
        Assert.Equal(feedCheck, result);
        _repository.Verify(r => r.SaveAsync(feedCheck), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var feedChecks = new List<FeedCheck> { new FeedCheck { Id = 1 } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(feedChecks);

        // Act
        var result = await _feedCheckManager.GetAllAsync();

        // Assert
        Assert.Equal(feedChecks, result);
        _repository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1 };
        _repository.Setup(r => r.DeleteAsync(feedCheck)).ReturnsAsync(true);

        // Act
        var result = await _feedCheckManager.DeleteAsync(feedCheck);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(feedCheck), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _feedCheckManager.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1, Name = "Test" };
        _repository.Setup(r => r.GetByNameAsync("Test")).ReturnsAsync(feedCheck);

        // Act
        var result = await _feedCheckManager.GetByNameAsync("Test");

        // Assert
        Assert.Equal(feedCheck, result);
        _repository.Verify(r => r.GetByNameAsync("Test"), Times.Once);
    }
}