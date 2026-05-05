using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Bluesky;

public class ProcessScheduledItemFiredTests
{
    private static EventGridEvent BuildEventGridEvent(int scheduledItemId)
    {
        var payload = JsonSerializer.Serialize(new ScheduledItemFiredEvent { Id = scheduledItemId });
        return new EventGridEvent("subject", "eventType", "1.0", BinaryData.FromString(payload));
    }

    private static Domain.Models.ScheduledItem BuildScheduledItem(
        int id = 1,
        int primaryKey = 42,
        ScheduledItemType itemType = ScheduledItemType.SyndicationFeedSources) => new()
    {
        Id = id,
        ItemType = itemType,
        ItemPrimaryKey = primaryKey,
        Message = "existing scheduled message",
        SendOnDateTime = DateTimeOffset.UtcNow
    };

    private static SyndicationFeedSource BuildFeedSource(int id = 42, string url = "https://example.com/post") => new()
    {
        Id = id,
        FeedIdentifier = "feed-1",
        Title = "Test Blog Post Title",
        Url = url,
        Author = "Author",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = ""
    };

    private static Engagement BuildEngagement(int id = 42, string url = "https://conf.example.com") => new()
    {
        Id = id,
        Name = "Tech Conference 2026",
        Url = url,
        StartDateTime = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
        EndDateTime = new DateTimeOffset(2026, 6, 3, 17, 0, 0, TimeSpan.Zero),
        TimeZoneId = "UTC"
    };

    private static Talk BuildTalk(int id = 42, int engagementId = 99, string url = "https://conf.example.com/talk") => new()
    {
        Id = id,
        EngagementId = engagementId,
        Name = "My Talk Title",
        UrlForConferenceTalk = url,
        UrlForTalk = url
    };

    private static YouTubeSource BuildYouTubeSource(int id = 42, string url = "https://youtube.com/watch?v=abc") => new()
    {
        Id = id,
        VideoId = "abc",
        Title = "My YouTube Video",
        Url = url,
        Author = "Author",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = ""
    };

    // Constructor order: (IScheduledItemManager, IEngagementManager, ISyndicationFeedSourceManager, IYouTubeSourceManager, IBlueskyManager, ILogger)
    private static Functions.Bluesky.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<IEngagementManager> engagementManager,
        Mock<ISyndicationFeedSourceManager> feedSourceManager,
        Mock<IYouTubeSourceManager> youTubeSourceManager,
        Mock<IBlueskyManager> blueskyManager)
    {
        return new Functions.Bluesky.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeSourceManager.Object,
            blueskyManager.Object,
            NullLogger<Functions.Bluesky.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_WhenEventDataIsNull_ReturnsNull()
    {
        var evt = new EventGridEvent("subject", "eventType", "1.0", BinaryData.FromString("null"));
        var sut = BuildSut(
            new Mock<IScheduledItemManager>(),
            new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(),
            new Mock<IBlueskyManager>());
        var result = await sut.RunAsync(evt);
        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_WhenComposeMessageReturnsText_ReturnsBlueskyPostMessage()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Blog Post Title");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title", result!.Text);
    }

    [Fact]
    public async Task RunAsync_WhenComposeMessageReturnsEmpty_ReturnsNull()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedSourceManager>(), new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_DelegatesTextCompositionToBlueskyManager()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Composed text");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        await sut.RunAsync(BuildEventGridEvent(1));
        mockBlueskyManager.Verify(m => m.ComposeMessageAsync(scheduledItem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsSyndicationFeed_SetsUrlFromFeedSource()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.SyndicationFeedSources);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource(url: "https://example.com/post"));
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Blog Post Title");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://example.com/post", result!.Url);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsYouTubeSource_SetsUrlFromYouTubeSource()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.YouTubeSources);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockYouTube = new Mock<IYouTubeSourceManager>();
        mockYouTube.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildYouTubeSource(url: "https://youtube.com/watch?v=abc"));
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("My YouTube Video");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedSourceManager>(), mockYouTube, mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://youtube.com/watch?v=abc", result!.Url);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsEngagement_SetsUrlFromEngagement()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Engagements);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockEngagement = new Mock<IEngagementManager>();
        mockEngagement.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildEngagement(url: "https://conf.example.com"));
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Speaking at Tech Conference 2026");
        var sut = BuildSut(mockScheduledItemManager, mockEngagement,
            new Mock<ISyndicationFeedSourceManager>(), new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com", result!.Url);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsTalk_SetsUrlFromTalk()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Talks);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockEngagement = new Mock<IEngagementManager>();
        mockEngagement.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(BuildTalk(url: "https://conf.example.com/talk"));
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("My Talk Title at Tech Conference 2026");
        var sut = BuildSut(mockScheduledItemManager, mockEngagement,
            new Mock<ISyndicationFeedSourceManager>(), new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com/talk", result!.Url);
    }

    [Fact]
    public async Task RunAsync_WhenImageUrlIsSet_ImageUrlAppearsInResult()
    {
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = "https://cdn.example.com/image.jpg";
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Blog Post Title");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://cdn.example.com/image.jpg", result!.ImageUrl);
    }

    [Fact]
    public async Task RunAsync_WhenImageUrlIsNull_ImageUrlIsNullInResult()
    {
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = null;
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var mockBlueskyManager = new Mock<IBlueskyManager>();
        mockBlueskyManager
            .Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Blog Post Title");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeSourceManager>(), mockBlueskyManager);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Null(result!.ImageUrl);
    }
}