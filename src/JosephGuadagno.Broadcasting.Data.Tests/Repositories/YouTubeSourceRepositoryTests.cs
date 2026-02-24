using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests.Repositories;

public class YouTubeSourceRepositoryTests
{
    private readonly Mock<IYouTubeSourceDataStore> _dataStoreMock;
    private readonly YouTubeSourceRepository _repository;

    public YouTubeSourceRepositoryTests()
    {
        _dataStoreMock = new Mock<IYouTubeSourceDataStore>();
        _repository = new YouTubeSourceRepository(_dataStoreMock.Object);
    }

    private static YouTubeSource CreateSource(int id = 1) =>
        new YouTubeSource
        {
            Id = id,
            VideoId = $"vid{id:0000}",
            Author = "Author",
            Title = "Title",
            Url = $"https://youtube.com/watch?v=vid{id:0000}",
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
        var sources = new List<YouTubeSource> { CreateSource(1), CreateSource(2) };
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
        _dataStoreMock.Setup(d => d.GetByUrlAsync("https://youtube.com/watch?v=vid0001")).ReturnsAsync(source);

        // Act
        var result = await _repository.GetByUrlAsync("https://youtube.com/watch?v=vid0001");

        // Assert
        Assert.Equal(source, result);
        _dataStoreMock.Verify(d => d.GetByUrlAsync("https://youtube.com/watch?v=vid0001"), Times.Once);
    }
}
