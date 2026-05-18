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

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class ProcessScheduledItemFiredTests
{
    private static EventGridEvent BuildEventGridEvent(int scheduledItemId)
    {
        var payload = JsonSerializer.Serialize(new ScheduledItemFiredEvent { Id = scheduledItemId });
        return new EventGridEvent("subject", "eventType", "1.0", BinaryData.FromString(payload));
    }

    private static ScheduledItem BuildScheduledItem(
        ScheduledItemType itemType = ScheduledItemType.SyndicationFeedItems,
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
        EngagementId = engagementId,
        Name = "Building .NET Apps",
        UrlForConferenceTalk = "https://conf.example.com/sessions/dotnet",
        UrlForTalk = "https://josephguadagno.net/talks/dotnet",
        Comments = "Great session",
        StartDateTime = DateTimeOffset.UtcNow,
        EndDateTime = DateTimeOffset.UtcNow.AddHours(1),
        TalkLocation = "Room A"
    };

    private static YouTubeItem BuildYouTubeItem() => new()
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

    // Constructor order: (IScheduledItemManager, IEngagementManager, ISyndicationFeedItemManager, IYouTubeItemManager, IUserOAuthTokenManager, IMessageTemplateLookup, IPostComposer, ILogger)
    private static Functions.LinkedIn.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<IEngagementManager> engagementManager,
        Mock<ISyndicationFeedItemManager> feedSourceManager,
        Mock<IYouTubeItemManager> youTubeItemManager,
        Mock<IUserOAuthTokenManager> userOAuthTokenManager,
        Mock<IMessageTemplateLookup> messageLookup,
        Mock<IPostComposer> postComposer)
    {
        return new Functions.LinkedIn.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeItemManager.Object,
            userOAuthTokenManager.Object,
            messageLookup.Object,
            postComposer.Object,
            NullLogger<Functions.LinkedIn.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_WhenComposeReturnsText_UsesReturnedText()
    {
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedItemManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var sut = BuildSut(
            mockScheduledItemManager,
            new Mock<IEngagementManager>(),
            mockFeedSourceManager,
            new Mock<IYouTubeItemManager>(),
            BuildUserOAuthTokenManager(),
            BuildTemplateLookup(),
            BuildPostComposer("Rendered from composer"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("Rendered from composer", result!.Text);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal(feedSource.Url, result.LinkUrl);
        Assert.Equal(feedSource.Title, result.Title);
        Assert.Equal(scheduledItem.ImageUrl, result.ImageUrl);
    }

    [Fact]
    public async Task RunAsync_WhenNoOAuthTokenFound_ReturnsNull()
    {
        var scheduledItem = BuildScheduledItem();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockTokenManager = new Mock<IUserOAuthTokenManager>();
        mockTokenManager.Setup(m => m.GetByUserAndPlatformAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserOAuthToken?)null);

        var mockComposer = BuildPostComposer();

        var sut = BuildSut(
            mockScheduledItemManager,
            new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedItemManager>(),
            new Mock<IYouTubeItemManager>(),
            mockTokenManager,
            BuildTemplateLookup(),
            mockComposer);

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.Null(result);
        mockComposer.Verify(
            m => m.ComposeAsync(It.IsAny<SocialMediaPublishRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsEngagements_UsesEngagementLinkAndTitle()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.Engagements);
        var engagement = BuildEngagement();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetAsync(42)).ReturnsAsync(engagement);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockEngagementManager,
            new Mock<ISyndicationFeedItemManager>(),
            new Mock<IYouTubeItemManager>(),
            BuildUserOAuthTokenManager(),
            BuildTemplateLookup(),
            BuildPostComposer("Engagement message"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("Engagement message", result!.Text);
        Assert.Equal(engagement.Name, result.Title);
        Assert.Equal(engagement.Url, result.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsTalks_UsesConferenceTalkLinkAndComposedText()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.Talks);
        var talk = BuildTalk();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(talk);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockEngagementManager,
            new Mock<ISyndicationFeedItemManager>(),
            new Mock<IYouTubeItemManager>(),
            BuildUserOAuthTokenManager(),
            BuildTemplateLookup(),
            BuildPostComposer("Talk message"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("Talk message", result!.Text);
        Assert.Equal(talk.Name, result.Title);
        Assert.Equal(talk.UrlForConferenceTalk, result.LinkUrl);
    }

    [Fact]
    public async Task RunAsync_WhenItemTypeIsYouTubeItems_UsesVideoUrlAndComposedText()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.YouTubeItems);
        var youTubeItem = BuildYouTubeItem();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockYouTubeItemManager = new Mock<IYouTubeItemManager>();
        mockYouTubeItemManager.Setup(m => m.GetAsync(42)).ReturnsAsync(youTubeItem);

        var sut = BuildSut(
            mockScheduledItemManager,
            new Mock<IEngagementManager>(),
            new Mock<ISyndicationFeedItemManager>(),
            mockYouTubeItemManager,
            BuildUserOAuthTokenManager(),
            BuildTemplateLookup(),
            BuildPostComposer("YouTube message"));

        var result = await sut.RunAsync(BuildEventGridEvent(1));

        Assert.NotNull(result);
        Assert.Equal("YouTube message", result!.Text);
        Assert.Equal(youTubeItem.Title, result.Title);
        // Phase 3: LinkUrl uses Url (not ShortenedUrl)
        Assert.Equal(youTubeItem.Url, result.LinkUrl);
    }
}
