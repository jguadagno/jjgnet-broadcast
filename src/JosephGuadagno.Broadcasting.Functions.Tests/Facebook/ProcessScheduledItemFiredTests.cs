using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
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

    private static Domain.Models.ScheduledItem BuildScheduledItem(int id = 1, int primaryKey = 42) => new()
    {
        Id = id,
        ItemType = ScheduledItemType.SyndicationFeedSources,
        ItemPrimaryKey = primaryKey,
        Message = "existing scheduled message",
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
        LastUpdatedOn = DateTimeOffset.UtcNow
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
        Tags = null,
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow
    };

    private static Functions.Facebook.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<ISyndicationFeedSourceManager> feedSourceManager,
        Mock<IYouTubeSourceManager> youTubeSourceManager,
        Mock<IEngagementManager> engagementManager,
        Mock<IMessageTemplateDataStore> messageTemplateDataStore)
    {
        return new Functions.Facebook.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeSourceManager.Object,
            messageTemplateDataStore.Object,
            NullLogger<Functions.Facebook.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_WhenTemplateFound_OverridesStatusTextWithRenderedText()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "Facebook",
            MessageType = "NewSyndicationFeedItem",
            Template = "{{ title }} - {{ url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewSyndicationFeedItem)).ReturnsAsync(messageTemplate);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — StatusText overridden by Scriban render; LinkUri still from the item
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title - https://example.com/post", result.StatusText);
        Assert.Equal("https://example.com/post", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_WhenTemplateIsNull_StatusTextUsesAutoGeneratedFallback()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewSyndicationFeedItem)).ReturnsAsync((MessageTemplate?)null);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — StatusText is the auto-generated "ICYMI: Blog Post: ..." fallback
        Assert.NotNull(result);
        Assert.Contains("ICYMI:", result.StatusText);
        Assert.Contains("Test Blog Post Title", result.StatusText);
        Assert.Equal("https://example.com/post", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_WhenTemplateRendersImageUrl_ImageUrlAppearsInStatusText()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = "https://cdn.example.com/image.jpg";

        var feedSource = BuildFeedSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "Facebook",
            MessageType = "NewSyndicationFeedItem",
            Template = "{{ title }} {{ image_url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewSyndicationFeedItem)).ReturnsAsync(messageTemplate);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("https://cdn.example.com/image.jpg", result.StatusText);
    }

    [Fact]
    public async Task RunAsync_WhenScheduledItemImageUrlIsNull_ImageUrlIsEmptyInRenderedStatusText()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = null;

        var feedSource = BuildFeedSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "Facebook",
            MessageType = "NewSyndicationFeedItem",
            Template = "{{ title }}|{{ image_url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewSyndicationFeedItem)).ReturnsAsync(messageTemplate);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — image_url renders as empty string
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title|", result.StatusText);
    }

    [Fact]
    public async Task RunAsync_LinkUri_IsAlwaysFromItemRegardlessOfTemplate()
    {
        // Arrange — template does NOT mention url, but LinkUri must still be the item's URL
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "Facebook",
            MessageType = "NewSyndicationFeedItem",
            Template = "Just the title: {{ title }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewSyndicationFeedItem)).ReturnsAsync(messageTemplate);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — LinkUri is always the item URL, never affected by template
        Assert.NotNull(result);
        Assert.Equal("https://example.com/post", result.LinkUri);
        Assert.Equal("Just the title: Test Blog Post Title", result.StatusText);
    }

    // ── Per-type tests: Facebook always uses RandomPost regardless of item type ──

    [Fact]
    public async Task RunAsync_WhenEngagementTemplateFound_RendersEngagementDataInStatusText()
    {
        // Arrange
        var scheduledItem = new Domain.Models.ScheduledItem
        {
            Id = 1, ItemType = ScheduledItemType.Engagements, ItemPrimaryKey = 42,
            Message = "engagement message", SendOnDateTime = DateTimeOffset.UtcNow
        };
        var engagement = BuildEngagement();
        var messageTemplate = new MessageTemplate
        {
            Platform = "Facebook",
            MessageType = MessageTemplates.MessageTypes.NewSpeakingEngagement,
            Template = "Speaking at {{ title }} - {{ url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetAsync(42)).ReturnsAsync(engagement);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewSpeakingEngagement))
            .ReturnsAsync(messageTemplate);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(), mockEngagementManager, mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — rendered via Scriban; LinkUri from the engagement
        Assert.NotNull(result);
        Assert.Equal("Speaking at Tech Conference 2026 - https://conf.example.com", result!.StatusText);
        Assert.Equal("https://conf.example.com", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_WhenEngagementTemplateIsNull_FallsBackToEngagementStatusText()
    {
        // Arrange
        var scheduledItem = new Domain.Models.ScheduledItem
        {
            Id = 1, ItemType = ScheduledItemType.Engagements, ItemPrimaryKey = 42,
            Message = "engagement message", SendOnDateTime = DateTimeOffset.UtcNow
        };
        var engagement = BuildEngagement();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetAsync(42)).ReturnsAsync(engagement);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewSpeakingEngagement))
            .ReturnsAsync((MessageTemplate?)null);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(), mockEngagementManager, mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — fallback includes engagement name and URL
        Assert.NotNull(result);
        Assert.Contains("Tech Conference 2026", result!.StatusText);
        Assert.Equal("https://conf.example.com", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_WhenTalkTemplateFound_RendersTalkDataInStatusText()
    {
        // Arrange
        var scheduledItem = new Domain.Models.ScheduledItem
        {
            Id = 1, ItemType = ScheduledItemType.Talks, ItemPrimaryKey = 42,
            Message = "talk message", SendOnDateTime = DateTimeOffset.UtcNow
        };
        var talk = BuildTalk();
        var messageTemplate = new MessageTemplate
        {
            Platform = "Facebook",
            MessageType = MessageTemplates.MessageTypes.ScheduledItem,
            Template = "My talk: {{ title }} - {{ url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(talk);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.ScheduledItem))
            .ReturnsAsync(messageTemplate);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(), mockEngagementManager, mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — rendered via Scriban using talk data
        Assert.NotNull(result);
        Assert.Equal("My talk: Building .NET Apps - https://josephguadagno.net/talks/dotnet", result!.StatusText);
        Assert.Equal("https://conf.example.com/talks/dotnet", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_WhenTalkTemplateIsNull_FallsBackToTalkStatusText()
    {
        // Arrange
        var scheduledItem = new Domain.Models.ScheduledItem
        {
            Id = 1, ItemType = ScheduledItemType.Talks, ItemPrimaryKey = 42,
            Message = "talk message", SendOnDateTime = DateTimeOffset.UtcNow
        };
        var talk = BuildTalk();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockEngagementManager = new Mock<IEngagementManager>();
        mockEngagementManager.Setup(m => m.GetTalkAsync(42)).ReturnsAsync(talk);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.ScheduledItem))
            .ReturnsAsync((MessageTemplate?)null);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            new Mock<IYouTubeSourceManager>(), mockEngagementManager, mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — fallback includes talk name; LinkUri is conference URL
        Assert.NotNull(result);
        Assert.Contains("Building .NET Apps", result!.StatusText);
        Assert.Equal("https://conf.example.com/talks/dotnet", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_WhenYouTubeTemplateFound_RendersVideoDataInStatusText()
    {
        // Arrange
        var scheduledItem = new Domain.Models.ScheduledItem
        {
            Id = 1, ItemType = ScheduledItemType.YouTubeSources, ItemPrimaryKey = 42,
            Message = "youtube message", SendOnDateTime = DateTimeOffset.UtcNow
        };
        var youTubeSource = BuildYouTubeSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "Facebook",
            MessageType = MessageTemplates.MessageTypes.NewYouTubeItem,
            Template = "New video: {{ title }} - {{ url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockYouTubeSourceManager = new Mock<IYouTubeSourceManager>();
        mockYouTubeSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(youTubeSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewYouTubeItem))
            .ReturnsAsync(messageTemplate);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            mockYouTubeSourceManager, new Mock<IEngagementManager>(), mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — rendered via Scriban; LinkUri is video URL
        Assert.NotNull(result);
        Assert.Equal("New video: Building Better Apps with .NET - https://youtube.com/watch?v=abc123def", result!.StatusText);
        Assert.Equal("https://youtube.com/watch?v=abc123def", result.LinkUri);
    }

    [Fact]
    public async Task RunAsync_WhenYouTubeTemplateIsNull_FallsBackToVideoStatusText()
    {
        // Arrange
        var scheduledItem = new Domain.Models.ScheduledItem
        {
            Id = 1, ItemType = ScheduledItemType.YouTubeSources, ItemPrimaryKey = 42,
            Message = "youtube message", SendOnDateTime = DateTimeOffset.UtcNow
        };
        var youTubeSource = BuildYouTubeSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockYouTubeSourceManager = new Mock<IYouTubeSourceManager>();
        mockYouTubeSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(youTubeSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.Facebook, MessageTemplates.MessageTypes.NewYouTubeItem))
            .ReturnsAsync((MessageTemplate?)null);

        var sut = BuildSut(mockScheduledItemManager, new Mock<ISyndicationFeedSourceManager>(),
            mockYouTubeSourceManager, new Mock<IEngagementManager>(), mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — fallback includes video title; LinkUri is video URL
        Assert.NotNull(result);
        Assert.Contains("Building Better Apps with .NET", result!.StatusText);
        Assert.Equal("https://youtube.com/watch?v=abc123def", result.LinkUri);
    }
}
