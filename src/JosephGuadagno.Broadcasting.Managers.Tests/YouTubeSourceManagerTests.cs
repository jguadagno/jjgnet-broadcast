using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class YouTubeSourceManagerTests
{
    private readonly Mock<IYouTubeSourceDataStore> _repository;
    private readonly YouTubeSourceManager _youTubeSourceManager;

    public YouTubeSourceManagerTests()
    {
        _repository = new Mock<IYouTubeSourceDataStore>();
        _youTubeSourceManager = new YouTubeSourceManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1, CreatedByEntraOid = "" };
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
        var source = new YouTubeSource { Id = 1, CreatedByEntraOid = "" };
        _repository.Setup(r => r.SaveAsync(source, default)).ReturnsAsync(OperationResult<YouTubeSource>.Success(source));

        // Act
        var result = await _youTubeSourceManager.SaveAsync(source);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(source, result.Value);
        _repository.Verify(r => r.SaveAsync(source, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var sources = new List<YouTubeSource> { new YouTubeSource { Id = 1, CreatedByEntraOid = "" } };
        _repository.Setup(r => r.GetAllAsync(default)).ReturnsAsync(sources);

        // Act
        var result = await _youTubeSourceManager.GetAllAsync();

        // Assert
        Assert.Equal(sources, result);
        _repository.Verify(r => r.GetAllAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1, CreatedByEntraOid = "" };
        _repository.Setup(r => r.DeleteAsync(source, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _youTubeSourceManager.DeleteAsync(source);

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
        var result = await _youTubeSourceManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetByUrlAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1, Url = "http://test.com", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetByUrlAsync("http://test.com")).ReturnsAsync(source);

        // Act
        var result = await _youTubeSourceManager.GetByUrlAsync("http://test.com");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByUrlAsync("http://test.com"), Times.Once);
    }

    [Fact]
    public async Task GetByVideoIdAsync_ShouldCallRepository()
    {
        // Arrange
        var source = new YouTubeSource { Id = 1, VideoId = "testvideoid", CreatedByEntraOid = "" };
        _repository.Setup(r => r.GetByVideoIdAsync("testvideoid")).ReturnsAsync(source);

        // Act
        var result = await _youTubeSourceManager.GetByVideoIdAsync("testvideoid");

        // Assert
        Assert.Equal(source, result);
        _repository.Verify(r => r.GetByVideoIdAsync("testvideoid"), Times.Once);
    }

    [Fact]
    public async Task GetByVideoIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetByVideoIdAsync("missing")).ReturnsAsync((YouTubeSource?)null);

        // Act
        var result = await _youTubeSourceManager.GetByVideoIdAsync("missing");

        // Assert
        Assert.Null(result);
        _repository.Verify(r => r.GetByVideoIdAsync("missing"), Times.Once);
    }
}