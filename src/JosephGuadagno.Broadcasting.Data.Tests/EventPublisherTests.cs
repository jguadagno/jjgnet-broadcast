using Azure.Messaging.EventGrid;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests;

public class EventPublisherTests
{
    private readonly Mock<IEventPublisherSettings> _settingsMock;
    private readonly Mock<ILogger<EventPublisher>> _loggerMock;
    private readonly TestableEventPublisher _publisher;

    public EventPublisherTests()
    {
        _settingsMock = new Mock<IEventPublisherSettings>();
        _loggerMock = new Mock<ILogger<EventPublisher>>();
        _publisher = new TestableEventPublisher(_settingsMock.Object, _loggerMock.Object);
    }

    private class TestableEventPublisher : EventPublisher
    {
        public Mock<EventGridPublisherClient> ClientMock { get; } = new();

        public TestableEventPublisher(IEventPublisherSettings settings, ILogger<EventPublisher> logger)
            : base(settings, logger)
        {
            InitialRetryDelay = TimeSpan.Zero;
        }

        protected override EventGridPublisherClient GetEventGridPublisherClient(ITopicEndpointSettings topicSettings)
        {
            return ClientMock.Object;
        }
    }

    private static ITopicEndpointSettings CreateTopicSettings(string topicName) =>
        Mock.Of<ITopicEndpointSettings>(t =>
            t.TopicName == topicName &&
            t.Endpoint == "https://example-topic.eastus-1.eventgrid.azure.net/api/events" &&
            t.Key == "fake-key");

    #region PublishSyndicationFeedEventsAsync

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_NullSubject_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "f1", Author = "a", Title = "t", Url = "https://example.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishSyndicationFeedEventsAsync(null!, items));
    }

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_EmptySubject_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "f1", Author = "a", Title = "t", Url = "https://example.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishSyndicationFeedEventsAsync(string.Empty, items));
    }

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_EmptyCollection_ReturnsFalse()
    {
        // Arrange
        var items = new List<SyndicationFeedSource>();

        // Act
        var result = await _publisher.PublishSyndicationFeedEventsAsync("subject", items);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_TopicNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings>());
        var items = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "f1", Author = "a", Title = "t", Url = "https://example.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _publisher.PublishSyndicationFeedEventsAsync("subject", items));
    }

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_Success_ReturnsTrue()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewSyndicationFeedItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "f1", Author = "a", Title = "t", Url = "https://example.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _publisher.PublishSyndicationFeedEventsAsync("subject", items);

        // Assert
        Assert.True(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_Exception_ReturnsFalse()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewSyndicationFeedItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "f1", Author = "a", Title = "t", Url = "https://example.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed"));

        // Act
        var result = await _publisher.PublishSyndicationFeedEventsAsync("subject", items);

        // Assert
        Assert.False(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_WhenFirstAttemptFails_RetriesAndSucceeds()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewSyndicationFeedItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "f1", Author = "a", Title = "t", Url = "https://example.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.SetupSequence(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Transient failure"))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _publisher.PublishSyndicationFeedEventsAsync("subject", items);

        // Assert
        Assert.True(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PublishSyndicationFeedEventsAsync_WhenAllAttemptsFailWithException_ReturnsFalse()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewSyndicationFeedItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<SyndicationFeedSource> { new SyndicationFeedSource { Id = 1, FeedIdentifier = "f1", Author = "a", Title = "t", Url = "https://example.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Persistent failure"));

        // Act
        var result = await _publisher.PublishSyndicationFeedEventsAsync("subject", items);

        // Assert
        Assert.False(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    #region PublishYouTubeEventsAsync

    [Fact]
    public async Task PublishYouTubeEventsAsync_NullSubject_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<YouTubeSource> { new YouTubeSource { Id = 1, VideoId = "abc123", Author = "a", Title = "t", Url = "https://youtube.com/watch?v=abc123", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishYouTubeEventsAsync(null!, items));
    }

    [Fact]
    public async Task PublishYouTubeEventsAsync_EmptySubject_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<YouTubeSource> { new YouTubeSource { Id = 1, VideoId = "abc123", Author = "a", Title = "t", Url = "https://youtube.com/watch?v=abc123", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishYouTubeEventsAsync(string.Empty, items));
    }

    [Fact]
    public async Task PublishYouTubeEventsAsync_EmptyCollection_ReturnsFalse()
    {
        // Arrange
        var items = new List<YouTubeSource>();

        // Act
        var result = await _publisher.PublishYouTubeEventsAsync("subject", items);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PublishYouTubeEventsAsync_TopicNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings>());
        var items = new List<YouTubeSource> { new YouTubeSource { Id = 1, VideoId = "abc123", Author = "a", Title = "t", Url = "https://youtube.com/watch?v=abc123", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _publisher.PublishYouTubeEventsAsync("subject", items));
    }

    [Fact]
    public async Task PublishYouTubeEventsAsync_Success_ReturnsTrue()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewYouTubeItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<YouTubeSource> { new YouTubeSource { Id = 1, VideoId = "v1", Author = "a", Title = "t", Url = "https://youtube.com/v1", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _publisher.PublishYouTubeEventsAsync("subject", items);

        // Assert
        Assert.True(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishYouTubeEventsAsync_Exception_ReturnsFalse()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewYouTubeItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<YouTubeSource> { new YouTubeSource { Id = 1, VideoId = "v1", Author = "a", Title = "t", Url = "https://youtube.com/v1", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed"));

        // Act
        var result = await _publisher.PublishYouTubeEventsAsync("subject", items);

        // Assert
        Assert.False(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task PublishYouTubeEventsAsync_WhenFirstAttemptFails_RetriesAndSucceeds()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewYouTubeItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<YouTubeSource> { new YouTubeSource { Id = 1, VideoId = "v1", Author = "a", Title = "t", Url = "https://youtube.com/v1", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.SetupSequence(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Transient failure"))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _publisher.PublishYouTubeEventsAsync("subject", items);

        // Assert
        Assert.True(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PublishYouTubeEventsAsync_WhenAllAttemptsFailWithException_ReturnsFalse()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewYouTubeItem);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<YouTubeSource> { new YouTubeSource { Id = 1, VideoId = "v1", Author = "a", Title = "t", Url = "https://youtube.com/v1", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Persistent failure"));

        // Act
        var result = await _publisher.PublishYouTubeEventsAsync("subject", items);

        // Assert
        Assert.False(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    #region PublishScheduledItemFiredEventsAsync

    [Fact]
    public async Task PublishScheduledItemFiredEventsAsync_NullSubject_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<ScheduledItem> { new ScheduledItem { Id = 1, ItemType = ScheduledItemType.SyndicationFeedSources, ItemPrimaryKey = 1, Message = "Msg", SendOnDateTime = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishScheduledItemFiredEventsAsync(null!, items));
    }

    [Fact]
    public async Task PublishScheduledItemFiredEventsAsync_EmptySubject_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<ScheduledItem> { new ScheduledItem { Id = 1, ItemType = ScheduledItemType.SyndicationFeedSources, ItemPrimaryKey = 1, Message = "Msg", SendOnDateTime = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishScheduledItemFiredEventsAsync(string.Empty, items));
    }

    [Fact]
    public async Task PublishScheduledItemFiredEventsAsync_EmptyCollection_ReturnsFalse()
    {
        // Arrange
        var items = new List<ScheduledItem>();

        // Act
        var result = await _publisher.PublishScheduledItemFiredEventsAsync("subject", items);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PublishScheduledItemFiredEventsAsync_TopicNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings>());
        var items = new List<ScheduledItem> { new ScheduledItem { Id = 1, ItemType = ScheduledItemType.SyndicationFeedSources, ItemPrimaryKey = 1, Message = "Msg", SendOnDateTime = DateTimeOffset.UtcNow } };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _publisher.PublishScheduledItemFiredEventsAsync("subject", items));
    }

    [Fact]
    public async Task PublishScheduledItemFiredEventsAsync_Success_ReturnsTrue()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.ScheduledItemFired);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<ScheduledItem> { new ScheduledItem { Id = 1, ItemType = ScheduledItemType.SyndicationFeedSources, ItemPrimaryKey = 1, Message = "m", SendOnDateTime = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _publisher.PublishScheduledItemFiredEventsAsync("subject", items);

        // Assert
        Assert.True(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishScheduledItemFiredEventsAsync_Exception_ReturnsFalse()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.ScheduledItemFired);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        var items = new List<ScheduledItem> { new ScheduledItem { Id = 1, ItemType = ScheduledItemType.SyndicationFeedSources, ItemPrimaryKey = 1, Message = "m", SendOnDateTime = DateTimeOffset.UtcNow } };
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed"));

        // Act
        var result = await _publisher.PublishScheduledItemFiredEventsAsync("subject", items);

        // Assert
        Assert.False(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    #region PublishRandomPostsEventsAsync

    [Fact]
    public async Task PublishRandomPostsEventsAsync_NullSubject_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishRandomPostsEventsAsync(null!, 1));
    }

    [Fact]
    public async Task PublishRandomPostsEventsAsync_EmptySubject_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishRandomPostsEventsAsync(string.Empty, 1));
    }

    [Fact]
    public async Task PublishRandomPostsEventsAsync_ZeroRandomPostId_ReturnsFalse()
    {
        // Act
        var result = await _publisher.PublishRandomPostsEventsAsync("subject", 0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PublishRandomPostsEventsAsync_NegativeRandomPostId_ReturnsFalse()
    {
        // Act
        var result = await _publisher.PublishRandomPostsEventsAsync("subject", -1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PublishRandomPostsEventsAsync_TopicNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _publisher.PublishRandomPostsEventsAsync("subject", 1));
    }

    [Fact]
    public async Task PublishRandomPostsEventsAsync_Success_ReturnsTrue()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewRandomPost);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _publisher.PublishRandomPostsEventsAsync("subject", 1);

        // Assert
        Assert.True(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishRandomPostsEventsAsync_Exception_ReturnsFalse()
    {
        // Arrange
        var topicSettings = CreateTopicSettings(Topics.NewRandomPost);
        _settingsMock.Setup(s => s.TopicEndpointSettings).Returns(new List<ITopicEndpointSettings> { topicSettings });
        _publisher.ClientMock.Setup(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed"));

        // Act
        var result = await _publisher.PublishRandomPostsEventsAsync("subject", 1);

        // Assert
        Assert.False(result);
        _publisher.ClientMock.Verify(c => c.SendEventsAsync(It.IsAny<IEnumerable<EventGridEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion
}