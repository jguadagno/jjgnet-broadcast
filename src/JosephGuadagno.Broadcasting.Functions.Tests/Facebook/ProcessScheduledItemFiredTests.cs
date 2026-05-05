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
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
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
        ScheduledItemType itemType = ScheduledItemType.SyndicationFeedSources) => new()
    {
        Id = id,
        ItemType = itemType,
        ItemPrimaryKey = primaryKey,
        Message = "scheduled message",
        SendOnDateTime = DateTimeOffset.UtcNow
    };

    private static SyndicationFeedSource BuildFeedSource() => new()
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
        CreatedByEntraOid = ""
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

    private static YouTubeSource BuildYouTubeSource(int id = 42) => new()
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
        CreatedByEntraOid = ""
    };

    private static Functions.Facebook.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<ISyndicationFeedSourceManager> feedSourceManager,
        Mock<IYouTubeSourceManager> youTubeSourceManager,
        Mock<IEngagementManager> engagementManager,
        Mock<IFacebookManager> facebookManager)
    {
        return new Functions.Facebook.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeSourceManager.Object,
            facebookManager.Object,
            NullLogger<Functions.Facebook.ProcessScheduledItemFired>.Instance);
    }

    private static Mock<IFacebookManager> BuildFacebookManager(string composedText = "composed message")
    {
        var mock = new Mock<IFacebookManager>();
        mock.Setup(m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(composedText);
        return mock;
    }

    [Fact]
    public async Task RunAsync_SyndicationSource_SetsStatusTextFromComposeMessageAsync()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var facebookManager = BuildFacebookManager("Test Blog Post Title - https://example.com/post");

        var sut = BuildSut(mockScheduledItemManager, mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(), new Mock<IEngagementManager>(), facebookManager);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — StatusText comes from ComposeMessageAsync; LinkUri is the feed URL
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title - https://example.com/post", result.StatusText);
        Assert.Equal("https://example.com/post", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_SyndicationSource_CallsComposeMessageAsync()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var facebookManager = BuildFacebookManager();

        var sut = BuildSut(mockScheduledItemManager, mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(), new Mock<IEngagementManager>(), facebookManager);

        // Act
        await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — ComposeMessageAsync was called with the scheduled item
        facebookManager.Verify(m => m.ComposeMessageAsync(
            It.Is<ScheduledItem>(s => s.Id == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_SyndicationSource_ImageUrlPropagated()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = "https://cdn.example.com/image.jpg";
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var sut = BuildSut(mockScheduledItemManager, mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(), new Mock<IEngagementManager>(), BuildFacebookManager());

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://cdn.example.com/image.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task RunAsync_Engagement_SetsLinkUriFromEngagementUrl()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Engagements);
        var engagement = BuildEngagement();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetAsync(42)).ReturnsAsync(engagement);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(), mockEngagementManager, BuildFacebookManager("engagement text"));

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — LinkUri is the engagement URL
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com", result.LinkUri);
        Assert.Equal("engagement text", result.StatusText);
    }

    [Fact]
    public async Task RunAsync_Talk_SetsLinkUriFromConferenceTalkUrl()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.Talks);
        var talk = BuildTalk();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(talk);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(), mockEngagementManager, BuildFacebookManager("talk text"));

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — LinkUri is UrlForConferenceTalk
        Assert.NotNull(result);
        Assert.Equal("https://conf.example.com/talks/dotnet", result.LinkUri);
        Assert.Equal("talk text", result.StatusText);
    }

    [Fact]
    public async Task RunAsync_YouTubeSource_SetsLinkUriFromVideoUrl()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem(itemType: ScheduledItemType.YouTubeSources);
        var youTubeSource = BuildYouTubeSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockYouTubeSourceManager = new Mock<IYouTubeSourceManager>();
        mockYouTubeSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(youTubeSource);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            mockYouTubeSourceManager, new Mock<IEngagementManager>(), BuildFacebookManager("video text"));

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://youtube.com/watch?v=abc123def", result.LinkUri);
        Assert.Equal("video text", result.StatusText);
    }

}
