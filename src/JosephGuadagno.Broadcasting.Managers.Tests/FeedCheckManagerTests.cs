using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class FeedCheckManagerTests
{
    private readonly Mock<IFeedCheckDataStore> _repository;
    private readonly FeedCheckManager _feedCheckManager;

    public FeedCheckManagerTests()
    {
        _repository = new Mock<IFeedCheckDataStore>();
        _feedCheckManager = new FeedCheckManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1 };
        _repository.Setup(r => r.GetAsync(1, default)).ReturnsAsync(feedCheck);

        // Act
        var result = await _feedCheckManager.GetAsync(1);

        // Assert
        Assert.Equal(feedCheck, result);
        _repository.Verify(r => r.GetAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1 };
        _repository.Setup(r => r.SaveAsync(feedCheck, default)).ReturnsAsync(OperationResult<FeedCheck>.Success(feedCheck));

        // Act
        var result = await _feedCheckManager.SaveAsync(feedCheck);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(feedCheck, result.Value);
        _repository.Verify(r => r.SaveAsync(feedCheck, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var feedChecks = new List<FeedCheck> { new FeedCheck { Id = 1 } };
        _repository.Setup(r => r.GetAllAsync(default)).ReturnsAsync(feedChecks);

        // Act
        var result = await _feedCheckManager.GetAllAsync();

        // Assert
        Assert.Equal(feedChecks, result);
        _repository.Verify(r => r.GetAllAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1 };
        _repository.Setup(r => r.DeleteAsync(feedCheck, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _feedCheckManager.DeleteAsync(feedCheck);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(feedCheck, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _feedCheckManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldCallRepository()
    {
        // Arrange
        var feedCheck = new FeedCheck { Id = 1, Name = "Test" };
        _repository.Setup(r => r.GetByNameAsync("Test", default)).ReturnsAsync(feedCheck);

        // Act
        var result = await _feedCheckManager.GetByNameAsync("Test");

        // Assert
        Assert.Equal(feedCheck, result);
        _repository.Verify(r => r.GetByNameAsync("Test", default), Times.Once);
    }
}