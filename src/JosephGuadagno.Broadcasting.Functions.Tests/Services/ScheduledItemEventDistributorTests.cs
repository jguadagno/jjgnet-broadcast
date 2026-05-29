using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using JosephGuadagno.Broadcasting.Composers;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Services;

public class ScheduledItemEventDistributorTests
{
    private const string OwnerOid = "scheduled-owner-oid";

    private readonly Mock<IUserEventDistributorMappingDataStore> _mappingDataStore;
    private readonly Mock<IEngagementManager> _engagementManager;
    private readonly Mock<ISyndicationFeedItemManager> _syndicationFeedItemManager;
    private readonly Mock<IYouTubeItemManager> _youTubeItemManager;
    private readonly Mock<IMessageTemplateManager> _messageTemplateManager;
    private readonly Mock<IPostComposer> _postComposer;
    private readonly Mock<QueueServiceClient> _queueServiceClient;
    private readonly Mock<QueueClient> _queueClient;
    private readonly ScheduledItemEventDistributor _sut;

    public ScheduledItemEventDistributorTests()
    {
        _mappingDataStore = new Mock<IUserEventDistributorMappingDataStore>();
        _engagementManager = new Mock<IEngagementManager>();
        _syndicationFeedItemManager = new Mock<ISyndicationFeedItemManager>();
        _youTubeItemManager = new Mock<IYouTubeItemManager>();
        _messageTemplateManager = new Mock<IMessageTemplateManager>();
        _postComposer = new Mock<IPostComposer>();
        _queueServiceClient = new Mock<QueueServiceClient>();
        _queueClient = new Mock<QueueClient>();

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

        _sut = new ScheduledItemEventDistributor(
            _mappingDataStore.Object,
            _engagementManager.Object,
            _syndicationFeedItemManager.Object,
            _youTubeItemManager.Object,
            _messageTemplateManager.Object,
            _postComposer.Object,
            _queueServiceClient.Object,
            NullLogger<ScheduledItemEventDistributor>.Instance);
    }

    private static UserEventDistributorMapping ActiveMapping(int platformId) => new()
    {
        Id = 1,
        CreatedByEntraOid = OwnerOid,
        EventType = MessageTemplates.MessageTypes.ScheduledItem,
        SocialMediaPlatformId = platformId,
        IsActive = true,
    };

    private static MessageTemplate BuildTemplate(int platformId) => new()
    {
        SocialMediaPlatformId = platformId,
        MessageType = MessageTemplates.MessageTypes.ScheduledItem,
        Template = "{{ title }} {{ link_url }}",
        CreatedByEntraOid = OwnerOid,
    };

    private static ScheduledItem BuildScheduledItem(ScheduledItemType itemType = ScheduledItemType.SyndicationFeedItems) => new()
    {
        Id = 10,
        ItemType = itemType,
        ItemPrimaryKey = 42,
        Message = "scheduled message",
        SendOnDateTime = DateTimeOffset.UtcNow,
        CreatedByEntraOid = OwnerOid,
        ImageUrl = "https://example.com/image.png",
    };

    private static SyndicationFeedItem BuildFeedItem() => new()
    {
        Id = 42,
        FeedIdentifier = "feed-1",
        Author = "Author",
        Title = "Scheduled blog post",
        Url = "https://example.com/post",
        ShortenedUrl = "https://short.example.com/post",
        Tags = ["dotnet"],
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = OwnerOid,
    };

    private static YouTubeItem BuildYouTubeItem() => new()
    {
        Id = 42,
        VideoId = "abc123",
        Author = "Author",
        Title = "Scheduled video",
        Url = "https://youtube.com/watch?v=abc123",
        ShortenedUrl = "https://short.example.com/video",
        Tags = ["video"],
        PublicationDate = DateTimeOffset.UtcNow,
        AddedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = OwnerOid,
    };

    private static Engagement BuildEngagement() => new()
    {
        Id = 42,
        Name = "Scheduled engagement",
        Url = "https://example.com/engagement",
        StartDateTime = DateTimeOffset.UtcNow,
        EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
        TimeZoneId = "UTC",
        CreatedByEntraOid = OwnerOid,
    };

    private static Talk BuildTalk() => new()
    {
        Id = 42,
        EngagementId = 99,
        Name = "Scheduled talk",
        UrlForConferenceTalk = "https://example.com/talk",
        UrlForTalk = "https://example.com/talk",
    };

    [Fact]
    public async Task DispatchAsync_WhenNoMappingsFound_DoesNotSendToQueue()
    {
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.ScheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDistributorMapping>());

        await _sut.DispatchAsync(BuildScheduledItem());

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_WhenFeedItemMappingAndTemplateFound_SendsToTwitterQueue()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.SyndicationFeedItems);
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.ScheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDistributorMapping> { ActiveMapping(SocialMediaPlatformIds.Twitter) });
        _syndicationFeedItemManager
            .Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildFeedItem());

        var template = BuildTemplate(SocialMediaPlatformIds.Twitter);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Twitter, MessageTemplates.MessageTypes.ScheduledItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _postComposer
            .Setup(c => c.ComposeAsync(It.Is<SocialMediaPublishRequest>(r =>
                    r.Title == "Scheduled blog post" &&
                    r.LinkUrl == "https://example.com/post" &&
                    r.ShortenedUrl == "https://short.example.com/post" &&
                    r.ImageUrl == "https://example.com/image.png"),
                template.Template,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Scheduled blog post https://example.com/post");

        await _sut.DispatchAsync(scheduledItem);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.TwitterTweetsToSend), Times.Once);
        Assert.Contains(_queueClient.Invocations, i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task DispatchAsync_WhenYouTubeItemMappingAndTemplateFound_SendsToLinkedInQueue()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.YouTubeItems);
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.ScheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDistributorMapping> { ActiveMapping(SocialMediaPlatformIds.LinkedIn) });
        _youTubeItemManager
            .Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildYouTubeItem());

        var template = BuildTemplate(SocialMediaPlatformIds.LinkedIn);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.LinkedIn, MessageTemplates.MessageTypes.ScheduledItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _postComposer
            .Setup(c => c.ComposeAsync(It.Is<SocialMediaPublishRequest>(r =>
                    r.Title == "Scheduled video" &&
                    r.LinkUrl == "https://youtube.com/watch?v=abc123" &&
                    r.ShortenedUrl == "https://short.example.com/video"),
                template.Template,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Scheduled video https://youtube.com/watch?v=abc123");

        await _sut.DispatchAsync(scheduledItem);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.LinkedInPostLink), Times.Once);
        Assert.Contains(_queueClient.Invocations, i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task DispatchAsync_WhenEngagementItemMappingAndTemplateFound_SendsToBlueskyQueue()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.Engagements);
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.ScheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDistributorMapping> { ActiveMapping(SocialMediaPlatformIds.Bluesky) });
        _engagementManager
            .Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEngagement());

        var template = BuildTemplate(SocialMediaPlatformIds.Bluesky);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Bluesky, MessageTemplates.MessageTypes.ScheduledItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _postComposer
            .Setup(c => c.ComposeAsync(It.Is<SocialMediaPublishRequest>(r =>
                    r.Title == "Scheduled engagement" &&
                    r.LinkUrl == "https://example.com/engagement"),
                template.Template,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Scheduled engagement https://example.com/engagement");

        await _sut.DispatchAsync(scheduledItem);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.BlueskyPostToSend), Times.Once);
        Assert.Contains(_queueClient.Invocations, i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task DispatchAsync_WhenTalkItemMappingAndTemplateFound_SendsToFacebookQueue()
    {
        var scheduledItem = BuildScheduledItem(ScheduledItemType.Talks);
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.ScheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDistributorMapping> { ActiveMapping(SocialMediaPlatformIds.Facebook) });
        _engagementManager
            .Setup(m => m.GetTalkAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTalk());

        var template = BuildTemplate(SocialMediaPlatformIds.Facebook);
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Facebook, MessageTemplates.MessageTypes.ScheduledItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _postComposer
            .Setup(c => c.ComposeAsync(It.Is<SocialMediaPublishRequest>(r =>
                    r.Title == "Scheduled talk" &&
                    r.LinkUrl == "https://example.com/talk"),
                template.Template,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Scheduled talk https://example.com/talk");

        await _sut.DispatchAsync(scheduledItem);

        _queueServiceClient.Verify(q => q.GetQueueClient(Queues.FacebookPostStatusToPage), Times.Once);
        Assert.Contains(_queueClient.Invocations, i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task DispatchAsync_WhenTemplateNotFound_DoesNotSendToQueue()
    {
        _mappingDataStore
            .Setup(s => s.GetByUserAndEventTypeAsync(OwnerOid, MessageTemplates.MessageTypes.ScheduledItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEventDistributorMapping> { ActiveMapping(SocialMediaPlatformIds.Twitter) });
        _syndicationFeedItemManager
            .Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildFeedItem());
        _messageTemplateManager
            .Setup(m => m.GetAsync(SocialMediaPlatformIds.Twitter, MessageTemplates.MessageTypes.ScheduledItem, OwnerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);

        await _sut.DispatchAsync(BuildScheduledItem());

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }
}
