using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class YouTubeItemDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly YouTubeItemDataStore _dataStore;

    public YouTubeItemDataStoreTests()
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

        _dataStore = new YouTubeItemDataStore(_context, mapper, NullLogger<YouTubeItemDataStore>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private YouTubeItem CreateYouTubeItem(string videoId = "abc123", string url = "https://youtube.com/watch?v=abc123") => new YouTubeItem
    {
        VideoId = videoId,
        Author = "Test Channel",
        Title = "My Test Video",
        Url = url,
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = ""
    };

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsYouTubeItem()
    {
        // Arrange
        var source = CreateYouTubeItem();
        _context.YouTubeItems.Add(source);
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
    public async Task GetAllAsync_ReturnsAllYouTubeItems()
    {
        // Arrange
        _context.YouTubeItems.AddRange(
            CreateYouTubeItem("vid1", "https://youtube.com/watch?v=vid1"),
            CreateYouTubeItem("vid2", "https://youtube.com/watch?v=vid2"),
            CreateYouTubeItem("vid3", "https://youtube.com/watch?v=vid3")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SaveAsync_NewYouTubeItem_SavesAndReturnsWithId()
    {
        // Arrange
        var domainSource = new Domain.Models.YouTubeItem
        {
            Id = 0,
            VideoId = "newvid",
            Author = "New Author",
            Title = "New Video",
            Url = "https://youtube.com/watch?v=newvid",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = ""
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
    public async Task SaveAsync_ExistingYouTubeItem_UpdatesAndReturns()
    {
        // Arrange
        var source = CreateYouTubeItem();
        _context.YouTubeItems.Add(source);
        await _context.SaveChangesAsync();
        _context.Entry(source).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var domainSource = new Domain.Models.YouTubeItem
        {
            Id = source.Id,
            VideoId = source.VideoId,
            Author = source.Author,
            Title = "Updated Title",
            Url = source.Url,
            PublicationDate = source.PublicationDate,
            AddedOn = source.AddedOn,
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = ""
        };

        // Act
        var result = await _dataStore.SaveAsync(domainSource);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Title", result.Value!.Title);
    }

    [Fact]
    public async Task DeleteAsync_WithYouTubeItemObject_DeletesSource()
    {
        // Arrange
        var source = CreateYouTubeItem();
        _context.YouTubeItems.Add(source);
        await _context.SaveChangesAsync();

        var domainSource = new Domain.Models.YouTubeItem { Id = source.Id, CreatedByEntraOid = "" };

        // Act
        var result = await _dataStore.DeleteAsync(domainSource);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.YouTubeItems.ToList());
    }

    [Fact]
    public async Task DeleteAsync_WithId_DeletesSource()
    {
        // Arrange
        var source = CreateYouTubeItem();
        _context.YouTubeItems.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(source.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.YouTubeItems.ToList());
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
    public async Task GetByUrlAsync_ExistingUrl_ReturnsYouTubeItem()
    {
        // Arrange
        var source = CreateYouTubeItem("myvid", "https://youtube.com/watch?v=myvid");
        _context.YouTubeItems.Add(source);
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
        var source = CreateYouTubeItem("testvid42", "https://youtube.com/watch?v=testvid42");
        _context.YouTubeItems.Add(source);
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

    [Fact]
    public async Task IsVideoUniqueToUser_ReturnsTrue_WhenVideoDoesNotExistForUser()
    {
        // Arrange — no items seeded

        // Act
        var result = await _dataStore.IsVideoUniqueToUser("vid1", "owner-1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsVideoUniqueToUser_ReturnsFalse_WhenVideoAlreadyExistsForUser()
    {
        // Arrange
        var item = CreateYouTubeItem("vid1", "https://youtube.com/watch?v=vid1");
        item.CreatedByEntraOid = "owner-1";
        _context.YouTubeItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.IsVideoUniqueToUser("vid1", "owner-1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsVideoUniqueToUser_ReturnsTrue_WhenSameVideoExistsForDifferentUser()
    {
        // Arrange — vid1 belongs to owner-1, not owner-2
        var item = CreateYouTubeItem("vid1", "https://youtube.com/watch?v=vid1");
        item.CreatedByEntraOid = "owner-1";
        _context.YouTubeItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.IsVideoUniqueToUser("vid1", "owner-2");

        // Assert — same videoId but different owner: unique for owner-2
        Assert.True(result);
    }

    [Fact]
    public async Task IsVideoUniqueToUser_ReturnsTrue_WhenDifferentVideoExistsForSameUser()
    {
        // Arrange — owner-1 has vid2, checking for vid1
        var item = CreateYouTubeItem("vid2", "https://youtube.com/watch?v=vid2");
        item.CreatedByEntraOid = "owner-1";
        _context.YouTubeItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.IsVideoUniqueToUser("vid1", "owner-1");

        // Assert
        Assert.True(result);
    }
}
