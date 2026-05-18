using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
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
        ScheduledItemType itemType = ScheduledItemType.SyndicationFeedItems) => new()
    {
        Id = id,
        ItemType = itemType,
        ItemPrimaryKey = primaryKey,
        Message = "existing scheduled message",
        SendOnDateTime = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid"
    };

    private static SyndicationFeedItem BuildFeedSource(int id = 42, string url = "https://example.com/post") => new()
    {
        Id = id,
        FeedIdentifier = "feed-1",
        Title = "Test Blog Post Title",
        Url = url,
        Author = "Author",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid"
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

    private static YouTubeItem BuildYouTubeItem(int id = 42, string url = "https://youtube.com/watch?v=abc") => new()
    {
        Id = id,
        VideoId = "abc",
        Title = "My YouTube Video",
        Url = url,
        Author = "Author",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid"
    };

    private static Mock<IMessageTemplateLookup> BuildTemplateLookup(string template = "template")
    {
        var mock = new Mock<IMessageTemplateLookup>();
        mock.Setup(m => m.GetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessageTemplate { Template = template });
        return mock;
    }

    private static Mock<IPostComposer> BuildPostComposer(string? composedText = "Composed post text")
    {
        var mock = new Mock<IPostComposer>();
        mock.Setup(m => m.ComposeAsync(
                It.IsAny<SocialMediaPublishRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(composedText);
        return mock;
    }

    // Constructor order: (IScheduledItemManager, IEngagementManager, ISyndicationFeedItemManager, IYouTubeItemManager, IMessageTemplateLookup, IPostComposer, ILogger)
    private static Functions.Bluesky.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<IEngagementManager> engagementManager,
        Mock<ISyndicationFeedItemManager> feedSourceManager,
        Mock<IYouTubeItemManager> youTubeItemManager,
        Mock<IMessageTemplateLookup> messageLookup,
        Mock<IPostComposer> postComposer)
    {
        return new Functions.Bluesky.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeItemManager.Object,
            messageLookup.Object,
            postComposer.Object,
            NullLogger<Functions.Bluesky.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_WhenEventDataIsNull_ReturnsNull()
    {
        var evt = new EventGridEvent("subject", "eventType", "1.0", BinaryData.FromString("null"));
        var sut = BuildSut(
            new Mock<IScheduledItemManager>(),
            new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedItemManager>(),
            new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(),
            BuildPostComposer());
        var result = await sut.RunAsync(evt);
        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_WhenComposeMessageReturnsText_ReturnsBlueskyPostMessage()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("Test Blog Post Title"));
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
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer(string.Empty));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_DelegatesTextCompositionToPostComposer()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var mockComposer = BuildPostComposer("Composed text");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(), BuildTemplateLookup(), mockComposer);
        await sut.RunAsync(BuildEventGridEvent(1));
        mockComposer.Verify(m => m.ComposeAsync(
            It.IsAny<SocialMediaPublishRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsSyndicationFeed_SetsUrlFromFeedSource()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.SyndicationFeedItems);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource(url: "https://example.com/post"));
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("Test Blog Post Title"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://example.com/post", result!.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsYouTubeItem_SetsUrlFromYouTubeItem()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.YouTubeItems);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockYouTube = new Mock<IYouTubeItemManager>();
        mockYouTube.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildYouTubeItem(url: "https://youtube.com/watch?v=abc"));
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedItemManager>(), mockYouTube,
            BuildTemplateLookup(), BuildPostComposer("My YouTube Video"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://youtube.com/watch?v=abc", result!.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsEngagement_SetsUrlFromEngagement()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Engagements);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockEngagement = new Mock<IEngagementManager>();
        mockEngagement.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildEngagement(url: "https://conf.example.com"));
        var sut = BuildSut(mockScheduledItemManager, mockEngagement,
            new Mock<ISyndicationFeedItemManager>(), new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("Speaking at Tech Conference 2026"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com", result!.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsTalk_SetsUrlFromTalk()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Talks);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockEngagement = new Mock<IEngagementManager>();
        mockEngagement.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(BuildTalk(url: "https://conf.example.com/talk"));
        var sut = BuildSut(mockScheduledItemManager, mockEngagement,
            new Mock<ISyndicationFeedItemManager>(), new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("My Talk Title at Tech Conference 2026"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com/talk", result!.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenImageUrlIsSet_ImageUrlAppearsInResult()
    {
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = "https://cdn.example.com/image.jpg";
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("Test Blog Post Title"));
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
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("Test Blog Post Title"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Null(result!.ImageUrl);
    }
}