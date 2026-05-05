using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Twitter.Tests;

public class TwitterManagerTests
{
    private readonly Mock<ILogger<TwitterManager>> _mockLogger;

    public TwitterManagerTests()
    {
        _mockLogger = new Mock<ILogger<TwitterManager>>();
    }

    private static TestableTwitterManager CreateSut(ILogger<TwitterManager> logger, Tweet? tweetResult, Exception? exception = null)
        => new(logger, tweetResult, exception);

    [Fact]
    public async Task SendTweetAsync_WhenTweetSucceeds_ReturnsTweetId()
    {
        // Arrange
        var expectedId = "123456789";
        var tweet = new Tweet { ID = expectedId };
        var sut = CreateSut(_mockLogger.Object, tweet);

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
        ISocialMediaPublisher sut = CreateSut(_mockLogger.Object, new Tweet { ID = expectedId });

        // Act
        var result = await sut.PublishAsync(new SocialMediaPublishRequest { Text = "Hello world!" });

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task PublishAsync_WhenTextIsMissing_ThrowsArgumentException()
    {
        // Arrange
        ISocialMediaPublisher sut = CreateSut(_mockLogger.Object, new Tweet { ID = "123456789" });

        // Act
        var act = () => sut.PublishAsync(new SocialMediaPublishRequest { Text = " " });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendTweetAsync_WhenTweetReturnsNull_ThrowsTwitterPostException()
    {
        // Arrange
        var sut = CreateSut(_mockLogger.Object, tweetResult: null);

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
        var sut = CreateSut(_mockLogger.Object, tweetResult: null, exception: new InvalidOperationException("Twitter API error"));

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
    public async Task ComposeMessageAsync_WhenServiceScopeFactoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = new TwitterManager(null!, _mockLogger.Object, null);
        var scheduledItem = new ScheduledItem { Message = "test message" };

        // Act
        var act = () => sut.ComposeMessageAsync(scheduledItem);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenPlatformNotFound_ReturnsFallbackMessage()
    {
        // Arrange
        var expectedMessage = "fallback message";
        var scheduledItem = new ScheduledItem { Message = expectedMessage };

        var mockPlatformManager = new Mock<ISocialMediaPlatformManager>();
        mockPlatformManager
            .Setup(m => m.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ISocialMediaPlatformManager)))
            .Returns(mockPlatformManager.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var sut = new TwitterManager(null!, _mockLogger.Object, mockScopeFactory.Object);

        // Act
        var result = await sut.ComposeMessageAsync(scheduledItem);

        // Assert
        result.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenValidScheduledItemWithTemplate_ReturnsNonEmptyString()
    {
        // Arrange
        var scheduledItem = new ScheduledItem
        {
            Message = "fallback",
            ItemType = ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 1
        };

        var platform = new SocialMediaPlatform { Id = 42, Name = MessageTemplates.Platforms.Twitter };
        var messageTemplate = new MessageTemplate { Template = "Check out: {{ title }} {{ url }}" };
        var feedSource = new SyndicationFeedSource { Title = "Test Post", Url = "https://example.com/post" };

        var mockPlatformManager = new Mock<ISocialMediaPlatformManager>();
        mockPlatformManager
            .Setup(m => m.GetByNameAsync(MessageTemplates.Platforms.Twitter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        var mockTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        mockTemplateDataStore
            .Setup(m => m.GetAsync(platform.Id, MessageTemplates.MessageTypes.NewSyndicationFeedItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageTemplate);

        var mockFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        mockFeedSourceManager
            .Setup(m => m.GetAsync(scheduledItem.ItemPrimaryKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedSource);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ISocialMediaPlatformManager)))
            .Returns(mockPlatformManager.Object);
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IMessageTemplateDataStore)))
            .Returns(mockTemplateDataStore.Object);
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ISyndicationFeedSourceManager)))
            .Returns(mockFeedSourceManager.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var sut = new TwitterManager(null!, _mockLogger.Object, mockScopeFactory.Object);

        // Act
        var result = await sut.ComposeMessageAsync(scheduledItem);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Test Post");
    }

    private sealed class TestableTwitterManager(ILogger<TwitterManager> logger, Tweet? tweetResult, Exception? exception = null)
        : TwitterManager(null!, logger)
    {
        protected override Task<Tweet?> TweetAsync(string tweetText)
        {
            if (exception is not null)
                throw exception;
            return Task.FromResult(tweetResult);
        }
    }
}
