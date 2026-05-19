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

namespace JosephGuadagno.Broadcasting.Functions.Tests.Facebook;

public class ProcessScheduledItemFiredTests
{
    private static EventGridEvent BuildEventGridEvent(int scheduledItemId)
    {
        var payload = JsonSerializer.Serialize(new ScheduledItemFiredEvent { Id = scheduledItemId });
        return new EventGridEvent("subject", "eventType", "1.0", BinaryData.FromString(payload));
    }

    private static Domain.Models.ScheduledItem BuildScheduledItem(int id = 1, int primaryKey = 42,
        ScheduledItemType itemType = ScheduledItemType.SyndicationFeedItems) => new()
    {
        Id = id,
        ItemType = itemType,
        ItemPrimaryKey = primaryKey,
        Message = "scheduled message",
        SendOnDateTime = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid"
    };

    private static SyndicationFeedItem BuildFeedSource() => new()
    {
        Id = 42,
        FeedIdentifier = "feed-1",
        Title = "Test Blog Post Title",
        Url = "https://example.com/post",
        ShortenedUrl = null,
        Author = "Author",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid"
    };

    private static Engagement BuildEngagement(int id = 42) => new()
    {
        Id = id,
        Name = "Tech Conference 2026",
        Url = "https://conf.example.com",
        StartDateTime = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
        EndDateTime = new DateTimeOffset(2026, 6, 3, 17, 0, 0, TimeSpan.Zero),
        TimeZoneId = "UTC",
        Comments = "Great event!"
    };

    private static Talk BuildTalk(int id = 42, int engagementId = 99) => new()
    {
        Id = id,
        Name = "Building .NET Apps",
        UrlForConferenceTalk = "https://conf.example.com/talks/dotnet",
        UrlForTalk = "https://josephguadagno.net/talks/dotnet",
        StartDateTime = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero),
        EndDateTime = new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero),
        TalkLocation = "Room A",
        Comments = "Excellent session",
        EngagementId = engagementId
    };

    private static YouTubeItem BuildYouTubeItem(int id = 42) => new()
    {
        Id = id,
        VideoId = "abc123def",
        Author = "Joseph Guadagno",
        Title = "Building Better Apps with .NET",
        Url = "https://youtube.com/watch?v=abc123def",
        ShortenedUrl = null,
        Tags = [],
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

    private static Mock<IPostComposer> BuildPostComposer(string? composedText = "composed message")
    {
        var mock = new Mock<IPostComposer>();
        mock.Setup(m => m.ComposeAsync(
                It.IsAny<SocialMediaPublishRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(composedText);
        return mock;
    }

    // Constructor order: (IScheduledItemManager, IEngagementManager, ISyndicationFeedItemManager, IYouTubeItemManager, IMessageTemplateLookup, IPostComposer, ILogger)
    private static Functions.Facebook.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<IEngagementManager> engagementManager,
        Mock<ISyndicationFeedItemManager> feedSourceManager,
        Mock<IYouTubeItemManager> youTubeItemManager,
        Mock<IMessageTemplateLookup> messageLookup,
        Mock<IPostComposer> postComposer)
    {
        return new Functions.Facebook.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeItemManager.Object,
            messageLookup.Object,
            postComposer.Object,
            NullLogger<Functions.Facebook.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_SyndicationSource_SetsStatusTextFromComposeAsync()
    {
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedItemManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSourceManager, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("Test Blog Post Title - https://example.com/post"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // StatusText comes from ComposeAsync; LinkUri comes from feed URL
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title - https://example.com/post", result.Text);
        Assert.Equal("https://example.com/post", result.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_SyndicationSource_CallsPostComposer()
    {
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedItemManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockComposer = BuildPostComposer();

        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSourceManager, new Mock<IYouTubeItemManager>(), BuildTemplateLookup(), mockComposer);

        await sut.RunAsync(BuildEventGridEvent(1));

        mockComposer.Verify(m => m.ComposeAsync(
            It.IsAny<SocialMediaPublishRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_SyndicationSource_ImageUrlPropagated()
    {
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = "https://cdn.example.com/image.jpg";
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedItemManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSourceManager, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer());

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("https://cdn.example.com/image.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task RunAsync_Engagement_SetsLinkUriFromEngagementUrl()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Engagements);
        var engagement = BuildEngagement();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetAsync(42)).ReturnsAsync(engagement);

        var sut = BuildSut(mockScheduledItemManager, mockEngagementManager,
            new Mock<ISyndicationFeedItemManager>(), new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("engagement text"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // LinkUri is the engagement URL
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com", result.LinkUrl);
        Assert.Equal("engagement text", result.Text);
    }

    [Fact]
    public async Task RunAsync_Talk_SetsLinkUriFromConferenceTalkUrl()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Talks);
        var talk = BuildTalk();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(talk);

        var sut = BuildSut(mockScheduledItemManager, mockEngagementManager,
            new Mock<ISyndicationFeedItemManager>(), new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer("talk text"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // LinkUri is UrlForConferenceTalk
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com/talks/dotnet", result.LinkUrl);
        Assert.Equal("talk text", result.Text);
    }

    [Fact]
    public async Task RunAsync_YouTubeItem_SetsLinkUriFromVideoUrl()
    {
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.YouTubeItems);
        var youTubeItem = BuildYouTubeItem();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockYouTubeItemManager = new Mock<IYouTubeItemManager>();
        mockYouTubeItemManager.Setup(m => m.GetAsync(42)).ReturnsAsync(youTubeItem);

        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedItemManager>(), mockYouTubeItemManager,
            BuildTemplateLookup(), BuildPostComposer("video text"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("https://youtube.com/watch?v=abc123def", result.LinkUrl);
        Assert.Equal("video text", result.Text);
    }

    [Fact]
    public async Task RunAsync_WhenComposeReturnsEmpty_ReturnsNull()
    {
        var scheduledItem = BuildScheduledItem();
        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);
        var mockFeedSourceManager = new Mock<ISyndicationFeedItemManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(BuildFeedSource());
        var sut = BuildSut(mockScheduledItemManager, new Mock<IEngagementManager>(),
            mockFeedSourceManager, new Mock<IYouTubeItemManager>(),
            BuildTemplateLookup(), BuildPostComposer(string.Empty));
        var result = await sut.RunAsync(BuildEventGridEvent(1));
        Assert.Null(result);
    }
}
