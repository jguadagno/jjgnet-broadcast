using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests.Repositories;

public class EngagementRepositoryTests
{
    private readonly Mock<IEngagementDataStore> _dataStoreMock;
    private readonly EngagementRepository _repository;

    public EngagementRepositoryTests()
    {
        _dataStoreMock = new Mock<IEngagementDataStore>();
        _repository = new EngagementRepository(_dataStoreMock.Object);
    }

    [Fact]
    public async Task GetAsync_WithPrimaryKey_DelegatesToDataStore()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        _dataStoreMock.Setup(d => d.GetAsync(1)).ReturnsAsync(engagement);

        // Act
        var result = await _repository.GetAsync(1);

        // Assert
        Assert.Equal(engagement, result);
        _dataStoreMock.Verify(d => d.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        _dataStoreMock.Setup(d => d.SaveAsync(engagement)).ReturnsAsync(engagement);

        // Act
        var result = await _repository.SaveAsync(engagement);

        // Assert
        Assert.Equal(engagement, result);
        _dataStoreMock.Verify(d => d.SaveAsync(engagement), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToDataStore()
    {
        // Arrange
        var engagements = new List<Engagement> { new Engagement { Id = 1 }, new Engagement { Id = 2 } };
        _dataStoreMock.Setup(d => d.GetAllAsync()).ReturnsAsync(engagements);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(engagements, result);
        _dataStoreMock.Verify(d => d.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        _dataStoreMock.Setup(d => d.DeleteAsync(engagement)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(engagement);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(engagement), Times.Once);
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
    public async Task GetTalksForEngagementAsync_DelegatesToDataStore()
    {
        // Arrange
        var talks = new List<Talk> { new Talk { Id = 1 }, new Talk { Id = 2 } };
        _dataStoreMock.Setup(d => d.GetTalksForEngagementAsync(1)).ReturnsAsync(talks);

        // Act
        var result = await _repository.GetTalksForEngagementAsync(1);

        // Assert
        Assert.Equal(talks, result);
        _dataStoreMock.Verify(d => d.GetTalksForEngagementAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveTalkAsync_WithTalk_DelegatesToDataStore()
    {
        // Arrange
        var talk = new Talk { Id = 1, Name = "My Talk" };
        _dataStoreMock.Setup(d => d.SaveTalkAsync(talk)).ReturnsAsync(talk);

        // Act
        var result = await _repository.SaveTalkAsync(talk);

        // Assert
        Assert.Equal(talk, result);
        _dataStoreMock.Verify(d => d.SaveTalkAsync(talk), Times.Once);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_WithTalkId_DelegatesToDataStore()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.RemoveTalkFromEngagementAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _repository.RemoveTalkFromEngagementAsync(1);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.RemoveTalkFromEngagementAsync(1), Times.Once);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_WithTalk_DelegatesToDataStore()
    {
        // Arrange
        var talk = new Talk { Id = 1 };
        _dataStoreMock.Setup(d => d.RemoveTalkFromEngagementAsync(talk)).ReturnsAsync(true);

        // Act
        var result = await _repository.RemoveTalkFromEngagementAsync(talk);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.RemoveTalkFromEngagementAsync(talk), Times.Once);
    }

    [Fact]
    public async Task GetTalkAsync_DelegatesToDataStore()
    {
        // Arrange
        var talk = new Talk { Id = 1, Name = "My Talk" };
        _dataStoreMock.Setup(d => d.GetTalkAsync(1)).ReturnsAsync(talk);

        // Act
        var result = await _repository.GetTalkAsync(1);

        // Assert
        Assert.Equal(talk, result);
        _dataStoreMock.Verify(d => d.GetTalkAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByNameAndUrlAndYearAsync_DelegatesToDataStore()
    {
        // Arrange
        var engagement = new Engagement { Id = 1, Name = "Conf" };
        _dataStoreMock.Setup(d => d.GetByNameAndUrlAndYearAsync("Conf", "https://conf.example.com", 2025)).ReturnsAsync(engagement);

        // Act
        var result = await _repository.GetByNameAndUrlAndYearAsync("Conf", "https://conf.example.com", 2025);

        // Assert
        Assert.Equal(engagement, result);
        _dataStoreMock.Verify(d => d.GetByNameAndUrlAndYearAsync("Conf", "https://conf.example.com", 2025), Times.Once);
    }
}
