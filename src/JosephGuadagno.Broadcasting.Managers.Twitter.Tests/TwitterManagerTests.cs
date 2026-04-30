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
