using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Twitter;

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
        Name = "Building .NET Apps",
        UrlForConferenceTalk = url,
        UrlForTalk = url
    };

    private static YouTubeItem BuildYouTubeItem(int id = 42, string url = "https://youtube.com/watch?v=abc123def") => new()
    {
        Id = id,
        VideoId = "abc123def",
        Title = "Building Better Apps with .NET",
        Url = url,
        Author = "Author",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid"
    };

    private static Mock<IMessageTemplateManager> BuildTemplateMock(string template = "template")
    {
        var mock = new Mock<IMessageTemplateManager>();
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

    // Constructor order: (IScheduledItemManager, IEngagementManager, ISyndicationFeedItemManager, IYouTubeItemManager, IMessageTemplateManager, IPostComposer, ILogger)
    private static Functions.Twitter.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<IEngagementManager> engagementManager,
        Mock<ISyndicationFeedItemManager> feedSourceManager,
        Mock<IYouTubeItemManager> youTubeItemManager,
        Mock<IMessageTemplateManager> messageTemplateManager,
        Mock<IPostComposer> postComposer)
    {
        return new Functions.Twitter.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeItemManager.Object,
            messageTemplateManager.Object,
            postComposer.Object,
            NullLogger<Functions.Twitter.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_WhenComposeReturnsText_ReturnsTweetWithText()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(),
            BuildTemplateMock(), BuildPostComposer("Test Blog Post Title - https://example.com/post"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title - https://example.com/post", result!.Text);
    }

    [Fact]
    public async Task RunAsync_WhenComposeReturnsEmpty_ReturnsNull()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(),
            BuildTemplateMock(), BuildPostComposer(string.Empty));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_WhenTemplateNotFound_ReturnsNull()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var mockLookup = new Mock<IMessageTemplateManager>();
        mockLookup.Setup(m => m.GetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(), mockLookup, BuildPostComposer());
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsSyndicationFeed_CallsPostComposer()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.SyndicationFeedItems);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSource = new Mock<ISyndicationFeedItemManager>();
        mockFeedSource.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var mockComposer = BuildPostComposer("Blog Post: Test Blog Post Title https://example.com/post");
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSource, new Mock<IYouTubeItemManager>(), BuildTemplateMock(), mockComposer);
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        mockComposer.Verify(m => m.ComposeAsync(
            It.IsAny<SocialMediaPublishRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsEngagements_ComposesWithEngagementData()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Engagements);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockEngagement = new Mock<IEngagementManager>();
        mockEngagement.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildEngagement());
        var sut = BuildSut(mockScheduledItemManager, mockEngagement,
            new Mock<ISyndicationFeedItemManager>(), new Mock<IYouTubeItemManager>(),
            BuildTemplateMock(), BuildPostComposer("Speaking at Tech Conference 2026 - https://conf.example.com"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Contains("Tech Conference 2026", result!.Text);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsTalks_ComposesWithTalkData()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Talks);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockEngagement = new Mock<IEngagementManager>();
        mockEngagement.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(BuildTalk());
        var sut = BuildSut(mockScheduledItemManager, mockEngagement,
            new Mock<ISyndicationFeedItemManager>(), new Mock<IYouTubeItemManager>(),
            BuildTemplateMock(), BuildPostComposer("My talk: Building .NET Apps - https://conf.example.com/talk"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Contains("Building .NET Apps", result!.Text);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsYouTubeItems_ComposesWithVideoData()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.YouTubeItems);
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockYouTube = new Mock<IYouTubeItemManager>();
        mockYouTube.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildYouTubeItem());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedItemManager>(), mockYouTube,
            BuildTemplateMock(), BuildPostComposer("New video: Building Better Apps with .NET - https://youtube.com/watch?v=abc123def"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Equal("New video: Building Better Apps with .NET - https://youtube.com/watch?v=abc123def", result!.Text);
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
            BuildTemplateMock(), BuildPostComposer("Tweet text"));
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
            BuildTemplateMock(), BuildPostComposer("Tweet text"));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.NotNull(result);
        Assert.Null(result!.ImageUrl);
    }

    [Fact]
    public async Task RunAsync_WhenOwnerEntraOidIsEmpty_ReturnsNull()
    {
        var scheduledItem = BuildScheduledItem();
        scheduledItem.CreatedByEntraOid = null;
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedItemManager>(), new Mock<IYouTubeItemManager>(),
            BuildTemplateMock(), BuildPostComposer());
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.Null(result);
    }
}
