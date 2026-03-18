using System;
using System.Text.Json;
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

    private static Mock<ILinkedInApplicationSettings> BuildLinkedInSettings()
    {
        var mock = new Mock<ILinkedInApplicationSettings>();
        mock.Setup(m => m.AuthorId).Returns("urn:li:person:test123");
        mock.Setup(m => m.AccessToken).Returns("test-access-token");
        mock.Setup(m => m.ClientId).Returns("client-id");
        mock.Setup(m => m.ClientSecret).Returns("client-secret");
        return mock;
    }

    private static Functions.LinkedIn.ProcessScheduledItemFired BuildSut(
        Mock<IScheduledItemManager> scheduledItemManager,
        Mock<ISyndicationFeedSourceManager> feedSourceManager,
        Mock<IYouTubeSourceManager> youTubeSourceManager,
        Mock<IEngagementManager> engagementManager,
        Mock<ILinkedInApplicationSettings> linkedInSettings,
        Mock<IMessageTemplateDataStore> messageTemplateDataStore)
    {
        return new Functions.LinkedIn.ProcessScheduledItemFired(
            scheduledItemManager.Object,
            engagementManager.Object,
            feedSourceManager.Object,
            youTubeSourceManager.Object,
            linkedInSettings.Object,
            messageTemplateDataStore.Object,
            NullLogger<Functions.LinkedIn.ProcessScheduledItemFired>.Instance);
    }

    [Fact]
    public async Task RunAsync_WhenTemplateFound_UsesRenderedTextAsPostText()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "LinkedIn",
            MessageType = "RandomPost",
            Template = "{{ title }} - {{ url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.LinkedIn, MessageTemplates.MessageTypes.RandomPost)).ReturnsAsync(messageTemplate);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            BuildLinkedInSettings(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — Text is the Scriban-rendered value
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title - https://example.com/post", result.Text);
    }

    [Fact]
    public async Task RunAsync_WhenTemplateIsNull_FallsBackToScheduledItemMessage()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.LinkedIn, MessageTemplates.MessageTypes.RandomPost)).ReturnsAsync((MessageTemplate?)null);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            BuildLinkedInSettings(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — LinkedIn fallback is scheduledItem.Message (not auto-generated text)
        Assert.NotNull(result);
        Assert.Equal("existing scheduled message", result.Text);
    }

    [Fact]
    public async Task RunAsync_WhenTemplateRendersImageUrl_ImageUrlAppearsInPostText()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = "https://cdn.example.com/image.jpg";

        var feedSource = BuildFeedSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "LinkedIn",
            MessageType = "RandomPost",
            Template = "{{ title }} {{ image_url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.LinkedIn, MessageTemplates.MessageTypes.RandomPost)).ReturnsAsync(messageTemplate);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            BuildLinkedInSettings(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert
        Assert.NotNull(result);
        Assert.Contains("https://cdn.example.com/image.jpg", result.Text);
    }

    [Fact]
    public async Task RunAsync_WhenScheduledItemImageUrlIsNull_ImageUrlIsEmptyInRenderedPostText()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        scheduledItem.ImageUrl = null;

        var feedSource = BuildFeedSource();
        var messageTemplate = new MessageTemplate
        {
            Platform = "LinkedIn",
            MessageType = "RandomPost",
            Template = "{{ title }}|{{ image_url }}"
        };

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.LinkedIn, MessageTemplates.MessageTypes.RandomPost)).ReturnsAsync(messageTemplate);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            BuildLinkedInSettings(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — image_url renders as empty string
        Assert.NotNull(result);
        Assert.Equal("Test Blog Post Title|", result.Text);
    }

    [Fact]
    public async Task RunAsync_AuthorIdAndAccessToken_AlwaysSetFromLinkedInSettings()
    {
        // Arrange
        var scheduledItem = BuildScheduledItem();
        var feedSource = BuildFeedSource();

        var mockScheduledItemManager = new Mock<IScheduledItemManager>();
        mockScheduledItemManager.Setup(m => m.GetAsync(1)).ReturnsAsync(scheduledItem);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager.Setup(m => m.GetAsync(42)).ReturnsAsync(feedSource);

        var mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockMessageTemplateDataStore.Setup(m => m.GetAsync(MessageTemplates.Platforms.LinkedIn, MessageTemplates.MessageTypes.RandomPost)).ReturnsAsync((MessageTemplate?)null);

        var sut = BuildSut(
            mockScheduledItemManager,
            mockFeedSourceManager,
            new Mock<IYouTubeSourceManager>(),
            new Mock<IEngagementManager>(),
            BuildLinkedInSettings(),
            mockMessageTemplateDataStore);

        // Act
        var result = await sut.RunAsync(BuildEventGridEvent(1));

        // Assert — credentials always come from settings, not from the template
        Assert.NotNull(result);
        Assert.Equal("urn:li:person:test123", result.AuthorId);
        Assert.Equal("test-access-token", result.AccessToken);
    }
}
