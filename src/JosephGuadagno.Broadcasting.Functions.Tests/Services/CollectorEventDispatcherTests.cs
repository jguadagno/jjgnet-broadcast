using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Services;

public class CollectorEventDispatcherTests
{
    private const string OwnerOid = "test-owner-oid";

    private readonly Mock<IUserEventDispatcherMappingDataStore> _mappingDataStore;
    private readonly Mock<IMessageTemplateManager> _messageTemplateManager;
    private readonly Mock<IPostComposer> _postComposer;
    private readonly Mock<QueueServiceClient> _queueServiceClient;
    private readonly Mock<QueueClient> _queueClient;
    private readonly CollectorEventDispatcher _sut;

    public CollectorEventDispatcherTests()
    {
        _mappingDataStore = new Mock<IUserEventDispatcherMappingDataStore>();
        _messageTemplateManager = new Mock<IMessageTemplateManager>();
        _postComposer = new Mock<IPostComposer>();
        _queueClient = new Mock<QueueClient>();
        _queueServiceClient = new Mock<QueueServiceClient>();

        _queueServiceClient
            .Setup(s => s.GetQueueClient(It.IsAny<string>()))
            .Returns(_queueClient.Object);

        var mockSendResponse = new Mock<Azure.Response<SendReceipt>>();
        _queueClient
            .Setup(q => q.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSendResponse.Object);

        _sut = new CollectorEventDispatcher(
            _mappingDataStore.Object,
            _messageTemplateManager.Object,
            _postComposer.Object,
            _queueServiceClient.Object,
            NullLogger<CollectorEventDispatcher>.Instance);
    }

    private static UserEventDispatcherMapping ActiveMapping(int platformId) => new()
    {
        Id = 1,
        CreatedByEntraOid = OwnerOid,
        EventType = MessageTemplates.MessageTypes.NewSyndicationFeedItem,
        SocialMediaPlatformId = platformId,
        IsActive = true,
    };

    private static MessageTemplate BuildTemplate(int platformId, string messageType) => new()
    {
        SocialMediaPlatformId = platformId,
        MessageType = messageType,
        Template = "{{ title }} {{ link_url }}",
        CreatedByEntraOid = OwnerOid,
    };

    private static SyndicationFeedItem BuildFeedItem() => new()
    {
        Id = 10,
        FeedIdentifier = "feed-1",
        Author = "Test Author",
        Title = "Test Post",
        Url = "https://example.com/post",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = OwnerOid,
    };

    private static YouTubeItem BuildYouTubeItem() => new()
    {
        Id = 20,
        VideoId = "abc123",
        Author = "Test Author",
        Title = "Test Video",
        Url = "https://youtu.be/abc123",
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = OwnerOid,
    };

    private static Engagement BuildEngagement() => new()
    {
        Id = 30,
        Name = "Test Conf",
        Url = "https://example.com/conf",
        StartDateTime = DateTimeOffset.UtcNow,
        EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
        TimeZoneId = "UTC",
        CreatedByEntraOid = OwnerOid,
    };

    // --- DispatchSyndicationFeedItemAsync ---

    [Fact]
    public async Task DispatchSyndicationFeedItemAsync_WhenNoMappingsFound_DoesNotSendToQueue()
    {
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewSyndicationFeedItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping>());

        await _sut.DispatchSyndicationFeedItemAsync(BuildFeedItem(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchSyndicationFeedItemAsync_WhenMappingAndTemplateFound_SendsToCorrectQueue()
    {
        var mapping = ActiveMapping(SocialMediaPlatformIds.Twitter);
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewSyndicationFeedItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping> { mapping });

        var template = BuildTemplate(SocialMediaPlatformIds.Twitter, MessageTemplates.MessageTypes.NewSyndicationFeedItem);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Twitter, MessageTemplates.MessageTypes.NewSyndicationFeedItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _postComposer
            .Setup(c => c.ComposeAsync(It.IsAny<SocialMediaPublishRequest>(), template.Template, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Post https://example.com/post");

        await _sut.DispatchSyndicationFeedItemAsync(BuildFeedItem(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.TwitterTweetsToSend), Times.Once);
        Assert.Contains(_queueClient.Invocations, i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task DispatchSyndicationFeedItemAsync_WhenTemplateNotFound_DoesNotSendToQueue()
    {
        var mapping = ActiveMapping(SocialMediaPlatformIds.Bluesky);
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewSyndicationFeedItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping> { mapping });

        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Bluesky, MessageTemplates.MessageTypes.NewSyndicationFeedItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);

        await _sut.DispatchSyndicationFeedItemAsync(BuildFeedItem(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    // --- DispatchYouTubeItemAsync ---

    [Fact]
    public async Task DispatchYouTubeItemAsync_WhenNoMappingsFound_DoesNotSendToQueue()
    {
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewYouTubeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping>());

        await _sut.DispatchYouTubeItemAsync(BuildYouTubeItem(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchYouTubeItemAsync_WhenMappingAndTemplateFound_SendsToCorrectQueue()
    {
        var mapping = new UserEventDispatcherMapping
        {
            Id = 2,
            CreatedByEntraOid = OwnerOid,
            EventType = MessageTemplates.MessageTypes.NewYouTubeItem,
            SocialMediaPlatformId = SocialMediaPlatformIds.LinkedIn,
            IsActive = true,
        };
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewYouTubeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping> { mapping });

        var template = BuildTemplate(SocialMediaPlatformIds.LinkedIn, MessageTemplates.MessageTypes.NewYouTubeItem);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.LinkedIn, MessageTemplates.MessageTypes.NewYouTubeItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _postComposer
            .Setup(c => c.ComposeAsync(It.IsAny<SocialMediaPublishRequest>(), template.Template, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Video https://youtu.be/abc123");

        await _sut.DispatchYouTubeItemAsync(BuildYouTubeItem(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.LinkedInPostLink), Times.Once);
        Assert.Contains(_queueClient.Invocations, i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task DispatchYouTubeItemAsync_WhenTemplateNotFound_DoesNotSendToQueue()
    {
        var mapping = new UserEventDispatcherMapping
        {
            Id = 2,
            CreatedByEntraOid = OwnerOid,
            EventType = MessageTemplates.MessageTypes.NewYouTubeItem,
            SocialMediaPlatformId = SocialMediaPlatformIds.Facebook,
            IsActive = true,
        };
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewYouTubeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping> { mapping });

        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Facebook, MessageTemplates.MessageTypes.NewYouTubeItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);

        await _sut.DispatchYouTubeItemAsync(BuildYouTubeItem(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    // --- DispatchSpeakingEngagementAsync ---

    [Fact]
    public async Task DispatchSpeakingEngagementAsync_WhenNoMappingsFound_DoesNotSendToQueue()
    {
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewSpeakingEngagement, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping>());

        await _sut.DispatchSpeakingEngagementAsync(BuildEngagement(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchSpeakingEngagementAsync_WhenMappingAndTemplateFound_SendsToCorrectQueue()
    {
        var mapping = new UserEventDispatcherMapping
        {
            Id = 3,
            CreatedByEntraOid = OwnerOid,
            EventType = MessageTemplates.MessageTypes.NewSpeakingEngagement,
            SocialMediaPlatformId = SocialMediaPlatformIds.Bluesky,
            IsActive = true,
        };
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewSpeakingEngagement, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping> { mapping });

        var template = BuildTemplate(SocialMediaPlatformIds.Bluesky, MessageTemplates.MessageTypes.NewSpeakingEngagement);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Bluesky, MessageTemplates.MessageTypes.NewSpeakingEngagement, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _postComposer
            .Setup(c => c.ComposeAsync(It.IsAny<SocialMediaPublishRequest>(), template.Template, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Conf https://example.com/conf");

        await _sut.DispatchSpeakingEngagementAsync(BuildEngagement(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.BlueskyPostToSend), Times.Once);
        Assert.Contains(_queueClient.Invocations, i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task DispatchSpeakingEngagementAsync_WhenTemplateNotFound_DoesNotSendToQueue()
    {
        var mapping = new UserEventDispatcherMapping
        {
            Id = 3,
            CreatedByEntraOid = OwnerOid,
            EventType = MessageTemplates.MessageTypes.NewSpeakingEngagement,
            SocialMediaPlatformId = SocialMediaPlatformIds.Twitter,
            IsActive = true,
        };
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewSpeakingEngagement, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping> { mapping });

        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Twitter, MessageTemplates.MessageTypes.NewSpeakingEngagement, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);

        await _sut.DispatchSpeakingEngagementAsync(BuildEngagement(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchSyndicationFeedItemAsync_WhenMultipleMappings_SendsToAllConfiguredQueues()
    {
        var twitterMapping = ActiveMapping(SocialMediaPlatformIds.Twitter);
        var blueskyMapping = new UserEventDispatcherMapping
        {
            Id = 2, CreatedByEntraOid = OwnerOid,
            EventType = MessageTemplates.MessageTypes.NewSyndicationFeedItem,
            SocialMediaPlatformId = SocialMediaPlatformIds.Bluesky, IsActive = true,
        };
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.NewSyndicationFeedItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDispatcherMapping> { twitterMapping, blueskyMapping });

        var twitterTemplate = BuildTemplate(SocialMediaPlatformIds.Twitter, MessageTemplates.MessageTypes.NewSyndicationFeedItem);
        var blueskyTemplate = BuildTemplate(SocialMediaPlatformIds.Bluesky, MessageTemplates.MessageTypes.NewSyndicationFeedItem);

        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Twitter, MessageTemplates.MessageTypes.NewSyndicationFeedItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(twitterTemplate);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Bluesky, MessageTemplates.MessageTypes.NewSyndicationFeedItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueskyTemplate);

        _postComposer
            .Setup(c => c.ComposeAsync(It.IsAny<SocialMediaPublishRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Post text");

        await _sut.DispatchSyndicationFeedItemAsync(BuildFeedItem(), OwnerOid);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.TwitterTweetsToSend), Times.Once);
        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.BlueskyPostToSend), Times.Once);
    }
}
