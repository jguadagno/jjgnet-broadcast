using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Twitter.Tests;

public class TwitterManagerTests
{
    private readonly Mock<ILogger<TwitterManager>> _mockLogger;
    private readonly Mock<ISocialMediaPlatformManager> _mockSocialMediaPlatformManager;
    private readonly Mock<IMessageTemplateDataStore> _mockMessageTemplateDataStore;
    private readonly Mock<ISyndicationFeedSourceManager> _mockSyndicationFeedSourceManager;
    private readonly Mock<IYouTubeSourceManager> _mockYouTubeSourceManager;
    private readonly Mock<IEngagementManager> _mockEngagementManager;

    public TwitterManagerTests()
    {
        _mockLogger = new Mock<ILogger<TwitterManager>>();
        _mockSocialMediaPlatformManager = new Mock<ISocialMediaPlatformManager>();
        _mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        _mockSyndicationFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        _mockYouTubeSourceManager = new Mock<IYouTubeSourceManager>();
        _mockEngagementManager = new Mock<IEngagementManager>();
    }

    private TestableTwitterManager CreateSut(Tweet? tweetResult, Exception? exception = null)
        => new(
            _mockLogger.Object,
            _mockSocialMediaPlatformManager.Object,
            _mockMessageTemplateDataStore.Object,
            _mockSyndicationFeedSourceManager.Object,
            _mockYouTubeSourceManager.Object,
            _mockEngagementManager.Object,
            tweetResult,
            exception);

    [Fact]
    public async Task SendTweetAsync_WhenTweetSucceeds_ReturnsTweetId()
    {
        // Arrange
        var expectedId = "123456789";
        var tweet = new Tweet { ID = expectedId };
        var sut = CreateSut(tweet);

        // Act
        var result = await sut.SendTweetAsync("Hello world!");

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task PublishAsync_WhenRequestIsValid_ReturnsTweetId()
    {
        // Arrange
        var expectedId = "123456789";
        ISocialMediaPublisher sut = CreateSut(new Tweet { ID = expectedId });

        // Act
        var result = await sut.PublishAsync(new SocialMediaPublishRequest { Text = "Hello world!" });

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task PublishAsync_WhenTextIsMissing_ThrowsArgumentException()
    {
        // Arrange
        ISocialMediaPublisher sut = CreateSut(new Tweet { ID = "123456789" });

        // Act
        var act = () => sut.PublishAsync(new SocialMediaPublishRequest { Text = " " });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendTweetAsync_WhenTweetReturnsNull_ThrowsTwitterPostException()
    {
        // Arrange
        var sut = CreateSut(tweetResult: null);

        // Act
        var act = () => sut.SendTweetAsync("Hello world!");

        // Assert
        await act.Should().ThrowAsync<TwitterPostException>()
            .WithMessage("*Hello world!*");
    }

    [Fact]
    public async Task SendTweetAsync_WhenExceptionThrown_ThrowsTwitterPostException()
    {
        // Arrange
        var sut = CreateSut(tweetResult: null, exception: new InvalidOperationException("Twitter API error"));

        // Act
        var act = () => sut.SendTweetAsync("Hello world!");

        // Assert
        await act.Should().ThrowAsync<TwitterPostException>()
            .WithMessage("*Hello world!*");
    }

    [Fact]
    public void TwitterPostException_IsA_BroadcastingException()
    {
        var exception = new TwitterPostException("test message");

        exception.Should().BeAssignableTo<BroadcastingException>();
    }

    [Fact]
    public void ITwitterManager_Implements_ISocialMediaPublisher()
    {
        typeof(ISocialMediaPublisher).IsAssignableFrom(typeof(ITwitterManager)).Should().BeTrue();
    }

    [Fact]
    public void ISocialMediaPublisher_DefinesPublishAsync_WithCommonRequestShape()
    {
        // Arrange
        var publishAsync = typeof(ISocialMediaPublisher).GetMethod(nameof(ISocialMediaPublisher.PublishAsync));

        // Assert
        publishAsync.Should().NotBeNull();
        publishAsync!.ReturnType.Should().Be(typeof(Task<string?>));
        publishAsync.GetParameters().Should().ContainSingle();
        publishAsync.GetParameters()[0].ParameterType.Should().Be(typeof(SocialMediaPublishRequest));
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenScheduledItemIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(tweetResult: null);

        // Act
        var act = async () => await sut.ComposeMessageAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenPlatformNotFound_ReturnsOriginalMessage()
    {
        // Arrange
        var expectedMessage = "Hello Twitter!";
        var scheduledItem = new ScheduledItem { Message = expectedMessage };
        _mockSocialMediaPlatformManager
            .Setup(m => m.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);
        var sut = CreateSut(tweetResult: null);

        // Act
        var result = await sut.ComposeMessageAsync(scheduledItem);

        // Assert
        result.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenTemplateNotFound_ReturnsOriginalMessage()
    {
        // Arrange
        var expectedMessage = "Hello Twitter!";
        var scheduledItem = new ScheduledItem { Message = expectedMessage };
        _mockSocialMediaPlatformManager
            .Setup(m => m.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SocialMediaPlatform { Id = 1, Name = "Twitter" });
        _mockMessageTemplateDataStore
            .Setup(m => m.GetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);
        var sut = CreateSut(tweetResult: null);

        // Act
        var result = await sut.ComposeMessageAsync(scheduledItem);

        // Assert
        result.Should().Be(expectedMessage);
    }

    private sealed class TestableTwitterManager(
        ILogger<TwitterManager> logger,
        ISocialMediaPlatformManager socialMediaPlatformManager,
        IMessageTemplateDataStore messageTemplateDataStore,
        ISyndicationFeedSourceManager syndicationFeedSourceManager,
        IYouTubeSourceManager youTubeSourceManager,
        IEngagementManager engagementManager,
        Tweet? tweetResult,
        Exception? exception = null)
        : TwitterManager(
            null!,
            logger,
            socialMediaPlatformManager,
            messageTemplateDataStore,
            syndicationFeedSourceManager,
            youTubeSourceManager,
            engagementManager)
    {
        protected override Task<Tweet?> TweetAsync(string tweetText)
        {
            if (exception is not null)
                throw exception;
            return Task.FromResult(tweetResult);
        }
    }
}