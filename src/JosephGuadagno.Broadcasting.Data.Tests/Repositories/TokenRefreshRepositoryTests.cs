using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests.Repositories;

public class TokenRefreshRepositoryTests
{
    private readonly Mock<ITokenRefreshDataStore> _dataStoreMock;
    private readonly TokenRefreshRepository _repository;

    public TokenRefreshRepositoryTests()
    {
        _dataStoreMock = new Mock<ITokenRefreshDataStore>();
        _repository = new TokenRefreshRepository(_dataStoreMock.Object);
    }

    private static TokenRefresh CreateToken(int id = 1) =>
        new TokenRefresh
        {
            Id = id,
            Name = $"Token{id}",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            LastChecked = DateTimeOffset.UtcNow,
            LastRefreshed = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task GetAsync_WithPrimaryKey_DelegatesToDataStore()
    {
        // Arrange
        var token = CreateToken();
        _dataStoreMock.Setup(d => d.GetAsync(1)).ReturnsAsync(token);

        // Act
        var result = await _repository.GetAsync(1);

        // Assert
        Assert.Equal(token, result);
        _dataStoreMock.Verify(d => d.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var token = CreateToken();
        _dataStoreMock.Setup(d => d.SaveAsync(token)).ReturnsAsync(token);

        // Act
        var result = await _repository.SaveAsync(token);

        // Assert
        Assert.Equal(token, result);
        _dataStoreMock.Verify(d => d.SaveAsync(token), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToDataStore()
    {
        // Arrange
        var tokens = new List<TokenRefresh> { CreateToken(1), CreateToken(2) };
        _dataStoreMock.Setup(d => d.GetAllAsync()).ReturnsAsync(tokens);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(tokens, result);
        _dataStoreMock.Verify(d => d.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var token = CreateToken();
        _dataStoreMock.Setup(d => d.DeleteAsync(token)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(token);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(token), Times.Once);
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
        var token = CreateToken();
        _dataStoreMock.Setup(d => d.GetByNameAsync("Token1")).ReturnsAsync(token);

        // Act
        var result = await _repository.GetByNameAsync("Token1");

        // Assert
        Assert.Equal(token, result);
        _dataStoreMock.Verify(d => d.GetByNameAsync("Token1"), Times.Once);
    }
}
