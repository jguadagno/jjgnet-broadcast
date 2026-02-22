using Moq;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class YouTubeSourceManagerTests
{
    private readonly Mock<IYouTubeSourceRepository> _repository;
    private readonly YouTubeSourceManager _youTubeSourceManager;

    public YouTubeSourceManagerTests()
    {
        _repository = new Mock<IYouTubeSourceRepository>();
        _youTubeSourceManager = new YouTubeSourceManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1 };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync(source);

        // Act
        var result = await _youTubeSourceManager.GetAsync(1);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1 };
        _repository.Setup(r => r.SaveAsync(source)).ReturnsAsync(source);

        // Act
        var result = await _youTubeSourceManager.SaveAsync(source);

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.SaveAsync(source), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var sources = new List<YouTubeSource> { new YouTubeSource { Id = 1 } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(sources);

        // Act
        var result = await _youTubeSourceManager.GetAllAsync();

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1 };
        _repository.Setup(r => r.DeleteAsync(source)).ReturnsAsync(true);

        // Act
        var result = await _youTubeSourceManager.DeleteAsync(source);

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
        var result = await _youTubeSourceManager.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByUrlAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1, Url = "http://test.com" };
        _repository.Setup(r => r.GetByUrlAsync("http://test.com")).ReturnsAsync(source);

        // Act
        var result = await _youTubeSourceManager.GetByUrlAsync("http://test.com");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByUrlAsync("http://test.com"), Times.Once);
    }
}