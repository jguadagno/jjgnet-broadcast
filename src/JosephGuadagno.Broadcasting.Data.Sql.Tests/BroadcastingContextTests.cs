using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class BroadcastingContextTests : IDisposable
{
    private readonly BroadcastingContext _context;

    public BroadcastingContextTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new BroadcastingContext(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void BroadcastingContext_DbSets_AreInitialized()
    {
        Assert.NotNull(_context.Engagements);
        Assert.NotNull(_context.ScheduledItems);
        Assert.NotNull(_context.Talks);
        Assert.NotNull(_context.FeedChecks);
        Assert.NotNull(_context.TokenRefreshes);
        Assert.NotNull(_context.SyndicationFeedSources);
        Assert.NotNull(_context.YouTubeSources);
    }

    [Fact]
    public async Task BroadcastingContext_AddEngagement_CanBeRetrieved()
    {
        // Arrange
        var engagement = new Engagement
        {
            Name = "Test Conference",
            Url = "https://example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC"
        };

        // Act
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Engagements.FindAsync(engagement.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Test Conference", retrieved.Name);
    }

    [Fact]
    public async Task BroadcastingContext_AddTalk_WithEngagement_MaintainsForeignKey()
    {
        // Arrange
        var engagement = new Engagement
        {
            Name = "Test Conf",
            Url = "https://example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC"
        };
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        var talk = new Talk
        {
            EngagementId = engagement.Id,
            Name = "My Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Act
        _context.Talks.Add(talk);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Talks.Include(t => t.Engagement).FirstOrDefaultAsync(t => t.Id == talk.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(engagement.Id, retrieved.EngagementId);
        Assert.Equal("Test Conf", retrieved.Engagement.Name);
    }

    [Fact]
    public async Task BroadcastingContext_AddScheduledItem_CanBeRetrieved()
    {
        // Arrange
        var item = new ScheduledItem
        {
            ItemTableName = "TestTable",
            ItemPrimaryKey = 42,
            Message = "Hello World",
            SendOnDateTime = DateTimeOffset.UtcNow.AddHours(1),
            MessageSent = false
        };

        // Act
        _context.ScheduledItems.Add(item);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.ScheduledItems.FindAsync(item.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("TestTable", retrieved.ItemTableName);
    }

    [Fact]
    public async Task BroadcastingContext_AddFeedCheck_CanBeRetrieved()
    {
        // Arrange
        var feedCheck = new FeedCheck
        {
            Name = "MyFeed",
            LastCheckedFeed = DateTimeOffset.UtcNow,
            LastItemAddedOrUpdated = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

        // Act
        _context.FeedChecks.Add(feedCheck);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.FeedChecks.FindAsync(feedCheck.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("MyFeed", retrieved.Name);
    }

    [Fact]
    public async Task BroadcastingContext_AddTokenRefresh_CanBeRetrieved()
    {
        // Arrange
        var tokenRefresh = new TokenRefresh
        {
            Name = "MyToken",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            LastChecked = DateTimeOffset.UtcNow,
            LastRefreshed = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

        // Act
        _context.TokenRefreshes.Add(tokenRefresh);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.TokenRefreshes.FindAsync(tokenRefresh.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("MyToken", retrieved.Name);
    }

    [Fact]
    public async Task BroadcastingContext_AddSyndicationFeedSource_CanBeRetrieved()
    {
        // Arrange
        var feedSource = new SyndicationFeedSource
        {
            FeedIdentifier = "feed-1",
            Author = "Test Author",
            Title = "Test Post",
            Url = "https://example.com/post",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = ""
        };

        // Act
        _context.SyndicationFeedSources.Add(feedSource);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.SyndicationFeedSources.FindAsync(feedSource.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("feed-1", retrieved.FeedIdentifier);
    }

    [Fact]
    public async Task BroadcastingContext_AddYouTubeSource_CanBeRetrieved()
    {
        // Arrange
        var youTubeSource = new YouTubeSource
        {
            VideoId = "abc123",
            Author = "Test Channel",
            Title = "My Video",
            Url = "https://youtube.com/watch?v=abc123",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = ""
        };

        // Act
        _context.YouTubeSources.Add(youTubeSource);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.YouTubeSources.FindAsync(youTubeSource.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("abc123", retrieved.VideoId);
    }
}
