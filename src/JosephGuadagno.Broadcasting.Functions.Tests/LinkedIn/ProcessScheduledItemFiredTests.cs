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
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class ProcessScheduledItemFiredTests
{
    private static EventGridEvent BuildEventGridEvent(int scheduledItemId)
    {
        var payload = JsonSerializer.Serialize(new ScheduledItemFiredEvent { Id = scheduledItemId });
        return new EventGridEvent("subject", "eventType", "1.0", BinaryData.FromString(payload));
    }

    private static ScheduledItem BuildScheduledItem(
        ScheduledItemType itemType = ScheduledItemType.SyndicationFeedSources,
        int id = 1,
        int primaryKey = 42) => new()
    {
        Id = id,
        ItemType = itemType,
        ItemPrimaryKey = primaryKey,
        Message = "existing scheduled message",
        SendOnDateTime = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid",
        ImageUrl = "https://cdn.example.com/image.jpg"
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
        EngagementId = engagementId,
        Name = "Building .NET Apps",
        UrlForConferenceTalk = "https://conf.example.com/sessions/dotnet",
        UrlForTalk = "https://josephguadagno.net/talks/dotnet",
        Comments = "Great session",
        StartDateTime = DateTimeOffset.UtcNow,
        EndDateTime = DateTimeOffset.UtcNow.AddHours(1),
        TalkLocation = "Room A"
    };

    private static YouTubeSource BuildYouTubeSource() => new()
    {
        Id = 42,
        Title = "Building Better Apps with .NET",
        Url = "https://youtube.com/watch?v=abc123def",
        ShortenedUrl = "https://youtu.be/abc123def",
        VideoId = "abc123def",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = "test-oid"
    };

    private static Mock<IUserOAuthTokenManager> BuildUserOAuthTokenManager(string accessToken = "test-access-token")
    {
        var mock = new Mock<IUserOAuthTokenManager>();
        mock.Setup(m => m.GetByUserAndPlatformAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOAuthToken
            {
                CreatedByEntraOid = "test-oid",
                SocialMediaPlatformId = SocialMediaPlatformIds.LinkedIn,
                AccessToken = accessToken,
                AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            });
        return mock;
    }

    private static Functions.LinkedIn.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<ISyndicationFeedSourceManager> feedSourceManager,
        Mock<IYouTubeSourceManager> youTubeSourceManager,
        Mock<IEngagementManager> engagementManager,
        Mock<IUserOAuthTokenManager> userOAuthTokenManager,
        Mock<ILinkedInManager> linkedInManager)
    {
        return new Functions.LinkedIn.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeSourceManager.Object,
            userOAuthTokenManager.Object,
            linkedInManager.Object,
            NullLogger<Functions.LinkedIn.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_WhenManagerComposesMessage_UsesReturnedText()
    {
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(feedSource);

        var mockLinkedInManager = new Mock<ILinkedInManager>();
        mockLinkedInManager
            .Setup(m => m.ComposeMessageAsync(scheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rendered from manager");

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            BuildUserOAuthTokenManager(),
            mockLinkedInManager);

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("Rendered from manager", result!.Text);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal(feedSource.Url, result.LinkUrl);
        Assert.Equal(feedSource.Title, result.Title);
        Assert.Equal(scheduledItem.ImageUrl, result.ImageUrl);
        mockLinkedInManager.Verify(
            m => m.ComposeMessageAsync(scheduledItem, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenNoOAuthTokenFound_ReturnsNull()
    {
        var scheduledItem = BuildScheduledItem();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(scheduledItem);

        var mockTokenManager = new Mock<IUserOAuthTokenManager>();
        mockTokenManager.Setup(m => m.GetByUserAndPlatformAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserOAuthToken?)null);

        var mockLinkedInManager = new Mock<ILinkedInManager>();

        var sut = BuildSut(
            mockScheduledItemManager,
            new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            mockTokenManager,
            mockLinkedInManager);

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.Null(result);
        mockLinkedInManager.Verify(
            m => m.ComposeMessageAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsEngagements_UsesEngagementLinkAndTitle()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.Engagements);
        var engagement = BuildEngagement();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(engagement);

        var mockLinkedInManager = new Mock<ILinkedInManager>();
        mockLinkedInManager
            .Setup(m => m.ComposeMessageAsync(scheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Engagement message");

        var sut = BuildSut(
            mockScheduledItemManager,
            new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(),
            mockEngagementManager,
            BuildUserOAuthTokenManager(),
            mockLinkedInManager);

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("Engagement message", result!.Text);
        Assert.Equal(engagement.Name, result.Title);
        Assert.Equal(engagement.Url, result.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsTalks_UsesConferenceTalkLinkAndManagerText()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.Talks);
        var talk = BuildTalk();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetTalkAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(talk);

        var mockLinkedInManager = new Mock<ILinkedInManager>();
        mockLinkedInManager
            .Setup(m => m.ComposeMessageAsync(scheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Talk message");

        var sut = BuildSut(
            mockScheduledItemManager,
            new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(),
            mockEngagementManager,
            BuildUserOAuthTokenManager(),
            mockLinkedInManager);

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("Talk message", result!.Text);
        Assert.Equal(talk.Name, result.Title);
        Assert.Equal(talk.UrlForConferenceTalk, result.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsYouTubeSources_UsesShortenedUrlAndManagerText()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.YouTubeSources);
        var youTubeSource = BuildYouTubeSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(scheduledItem);

        var mockYouTubeSourceManager = new Mock<IYouTubeSourceManager>();
        mockYouTubeSourceManager.Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(youTubeSource);

        var mockLinkedInManager = new Mock<ILinkedInManager>();
        mockLinkedInManager
            .Setup(m => m.ComposeMessageAsync(scheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync("YouTube message");

        var sut = BuildSut(
            mockScheduledItemManager,
            new Mock<ISyndicationFeedSourceManager>(),
            mockYouTubeSourceManager,
            new Mock<IEngagementManager>(),
            BuildUserOAuthTokenManager(),
            mockLinkedInManager);

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("YouTube message", result!.Text);
        Assert.Equal(youTubeSource.Title, result.Title);
        Assert.Equal(youTubeSource.ShortenedUrl, result.LinkUrl);
    }
}
