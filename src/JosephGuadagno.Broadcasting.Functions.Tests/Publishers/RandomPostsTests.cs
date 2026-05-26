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
using JosephGuadagno.Broadcasting.Functions.Publishers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Publishers;

public class RandomPostsTests
{
    private const string OwnerOid = "test-owner-oid";

    private readonly Mock<IUserRandomPostSettingsDataStore> _settingsDataStore;
    private readonly Mock<ISyndicationFeedItemManager> _feedItemManager;
    private readonly Mock<IMessageTemplateManager> _messageTemplateManager;
    private readonly Mock<IPostComposer> _postComposer;
    private readonly Mock<QueueServiceClient> _queueServiceClient;
    private readonly Mock<QueueClient> _queueClient;
    private readonly RandomPosts _sut;

    private static readonly TimerInfo FakeTimer = new();

    public RandomPostsTests()
    {
        _settingsDataStore = new Mock<IUserRandomPostSettingsDataStore>();
        _feedItemManager = new Mock<ISyndicationFeedItemManager>();
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

        _sut = new RandomPosts(
            _settingsDataStore.Object,
            _feedItemManager.Object,
            _messageTemplateManager.Object,
            _postComposer.Object,
            _queueServiceClient.Object,
            NullLogger<RandomPosts>.Instance);
    }

    private static UserRandomPostSettings ActiveSettings(
        string ownerOid = OwnerOid,
        int platformId = SocialMediaPlatformIds.Twitter,
        string cronExpression = "* * * * *") =>
        new()
        {
            Id = 1,
            CreatedByEntraOid = ownerOid,
            SocialMediaPlatformId = platformId,
            CronExpression = cronExpression,
            IsActive = true,
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow,
        };

    private static SyndicationFeedItem BuildFeedItem() =>
        new()
        {
            Id = 42,
            FeedIdentifier = "feed-1",
            Author = "Test Author",
            Title = "Test Post Title",
            Url = "https://example.com/post",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = OwnerOid,
        };

    [Fact]
    public async Task RunAsync_WhenNoActiveSettings_DoesNotDispatch()
    {
        _settingsDataStore
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRandomPostSettings>());

        await _sut.RunAsync(FakeTimer);

        _feedItemManager.Verify(
            m => m.GetRandomSyndicationDataAsync(
                It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenCronNotDue_DoesNotDispatch()
    {
        // Feb 31 never occurs — Cronos GetNextOccurrence returns null.
        var settings = ActiveSettings(cronExpression: "0 0 31 2 *");
        _settingsDataStore
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRandomPostSettings> { settings });

        await _sut.RunAsync(FakeTimer);

        _feedItemManager.Verify(
            m => m.GetRandomSyndicationDataAsync(
                It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenCronDueAndItemFound_DispatchesToCorrectQueue()
    {
        // "* * * * *" fires every minute — guaranteed to be due.
        var settings = ActiveSettings(
            cronExpression: "* * * * *",
            platformId: SocialMediaPlatformIds.Twitter);

        _settingsDataStore
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRandomPostSettings> { settings });

        var feedItem = BuildFeedItem();
        _feedItemManager
            .Setup(m => m.GetRandomSyndicationDataAsync(
                OwnerOid, It.IsAny<DateTimeOffset>(),
                It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedItem);

        var template = new MessageTemplate
        {
            SocialMediaPlatformId = SocialMediaPlatformIds.Twitter,
            MessageType = MessageTemplates.MessageTypes.RandomPost,
            Template = "{{ title }} {{ link_url }}",
            CreatedByEntraOid = OwnerOid,
        };
        _messageTemplateManager
            .Setup(m => m.GetAsync(
                SocialMediaPlatformIds.Twitter,
                MessageTemplates.MessageTypes.RandomPost,
                OwnerOid,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _postComposer
            .Setup(c => c.ComposeAsync(
                It.IsAny<SocialMediaPublishRequest>(),
                template.Template,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Post Title https://example.com/post");

        await _sut.RunAsync(FakeTimer);

        _queueServiceClient.Verify(
            q => q.GetQueueClient(Queues.TwitterTweetsToSend),
            Times.Once);
        Assert.Contains(
            _queueClient.Invocations,
            i => i.Method.Name == nameof(QueueClient.SendMessageAsync));
    }

    [Fact]
    public async Task RunAsync_WhenDataStoreThrows_PropagatesException()
    {
        _settingsDataStore
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RunAsync(FakeTimer));

        _queueServiceClient.Verify(q => q.GetQueueClient(It.IsAny<string>()), Times.Never);
    }
}
