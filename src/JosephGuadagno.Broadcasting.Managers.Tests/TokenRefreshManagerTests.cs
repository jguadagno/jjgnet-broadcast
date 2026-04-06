using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class TokenRefreshManagerTests
{
    private readonly Mock<ITokenRefreshDataStore> _repository;
    private readonly TokenRefreshManager _tokenRefreshManager;

    public TokenRefreshManagerTests()
    {
        _repository = new Mock<ITokenRefreshDataStore>();
        _tokenRefreshManager = new TokenRefreshManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var tokenRefresh = new TokenRefresh { Id = 1 };
        _repository.Setup(r => r.GetAsync(1, default)).ReturnsAsync(tokenRefresh);

        // Act
        var result = await _tokenRefreshManager.GetAsync(1);

        // Assert
        Assert.Equal(tokenRefresh, result);
        _repository.Verify(r => r.GetAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var tokenRefresh = new TokenRefresh { Id = 1 };
        _repository.Setup(r => r.SaveAsync(tokenRefresh, default)).ReturnsAsync(OperationResult<TokenRefresh>.Success(tokenRefresh));

        // Act
        var result = await _tokenRefreshManager.SaveAsync(tokenRefresh);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tokenRefresh, result.Value);
        _repository.Verify(r => r.SaveAsync(tokenRefresh, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var tokenRefreshes = new List<TokenRefresh> { new TokenRefresh { Id = 1 } };
        _repository.Setup(r => r.GetAllAsync(default)).ReturnsAsync(tokenRefreshes);

        // Act
        var result = await _tokenRefreshManager.GetAllAsync();

        // Assert
        Assert.Equal(tokenRefreshes, result);
        _repository.Verify(r => r.GetAllAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var tokenRefresh = new TokenRefresh { Id = 1 };
        _repository.Setup(r => r.DeleteAsync(tokenRefresh, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _tokenRefreshManager.DeleteAsync(tokenRefresh);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(tokenRefresh, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _tokenRefreshManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldCallRepository()
    {
        // Arrange
        var tokenRefresh = new TokenRefresh { Id = 1, Name = "Test" };
        _repository.Setup(r => r.GetByNameAsync("Test", default)).ReturnsAsync(tokenRefresh);

        // Act
        var result = await _tokenRefreshManager.GetByNameAsync("Test");

        // Assert
        Assert.Equal(tokenRefresh, result);
        _repository.Verify(r => r.GetByNameAsync("Test", default), Times.Once);
    }
}