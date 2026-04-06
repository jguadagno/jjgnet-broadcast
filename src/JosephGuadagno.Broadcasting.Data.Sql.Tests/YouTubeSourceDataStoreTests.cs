using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class YouTubeSourceDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly YouTubeSourceDataStore _dataStore;

    public YouTubeSourceDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new BroadcastingContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var mapper = config.CreateMapper();

        _dataStore = new YouTubeSourceDataStore(_context, mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private YouTubeSource CreateYouTubeSource(string videoId = "abc123", string url = "https://youtube.com/watch?v=abc123") => new YouTubeSource
    {
        VideoId = videoId,
        Author = "Test Channel",
        Title = "My Test Video",
        Url = url,
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsYouTubeSource()
    {
        // Arrange
        var source = CreateYouTubeSource();
        _context.YouTubeSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(source.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(source.Id, result.Id);
        Assert.Equal("abc123", result.VideoId);
    }

    [Fact]
    public async Task GetAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllYouTubeSources()
    {
        // Arrange
        _context.YouTubeSources.AddRange(
            CreateYouTubeSource("vid1", "https://youtube.com/watch?v=vid1"),
            CreateYouTubeSource("vid2", "https://youtube.com/watch?v=vid2"),
            CreateYouTubeSource("vid3", "https://youtube.com/watch?v=vid3")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SaveAsync_NewYouTubeSource_SavesAndReturnsWithId()
    {
        // Arrange
        var domainSource = new Domain.Models.YouTubeSource
        {
            Id = 0,
            VideoId = "newvid",
            Author = "New Author",
            Title = "New Video",
            Url = "https://youtube.com/watch?v=newvid",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _dataStore.SaveAsync(domainSource);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal("New Video", result.Value!.Title);
    }

    [Fact]
    public async Task SaveAsync_ExistingYouTubeSource_UpdatesAndReturns()
    {
        // Arrange
        var source = CreateYouTubeSource();
        _context.YouTubeSources.Add(source);
        await _context.SaveChangesAsync();
        _context.Entry(source).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var domainSource = new Domain.Models.YouTubeSource
        {
            Id = source.Id,
            VideoId = source.VideoId,
            Author = source.Author,
            Title = "Updated Title",
            Url = source.Url,
            PublicationDate = source.PublicationDate,
            AddedOn = source.AddedOn,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _dataStore.SaveAsync(domainSource);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Title", result.Value!.Title);
    }

    [Fact]
    public async Task DeleteAsync_WithYouTubeSourceObject_DeletesSource()
    {
        // Arrange
        var source = CreateYouTubeSource();
        _context.YouTubeSources.Add(source);
        await _context.SaveChangesAsync();

        var domainSource = new Domain.Models.YouTubeSource { Id = source.Id };

        // Act
        var result = await _dataStore.DeleteAsync(domainSource);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.YouTubeSources.ToList());
    }

    [Fact]
    public async Task DeleteAsync_WithId_DeletesSource()
    {
        // Arrange
        var source = CreateYouTubeSource();
        _context.YouTubeSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(source.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.YouTubeSources.ToList());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsTrue()
    {
        // Act
        var result = await _dataStore.DeleteAsync(999);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetByUrlAsync_ExistingUrl_ReturnsYouTubeSource()
    {
        // Arrange
        var source = CreateYouTubeSource("myvid", "https://youtube.com/watch?v=myvid");
        _context.YouTubeSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByUrlAsync("https://youtube.com/watch?v=myvid");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("myvid", result.VideoId);
    }

    [Fact]
    public async Task GetByUrlAsync_NonExistingUrl_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetByUrlAsync("https://youtube.com/watch?v=nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByVideoIdAsync_ReturnsRecord_WhenVideoIdExists()
    {
        // Arrange
        var source = CreateYouTubeSource("testvid42", "https://youtube.com/watch?v=testvid42");
        _context.YouTubeSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByVideoIdAsync("testvid42");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testvid42", result.VideoId);
    }

    [Fact]
    public async Task GetByVideoIdAsync_ReturnsNull_WhenVideoIdDoesNotExist()
    {
        // Act
        var result = await _dataStore.GetByVideoIdAsync("nonexistent-video-id");

        // Assert
        Assert.Null(result);
    }
}
