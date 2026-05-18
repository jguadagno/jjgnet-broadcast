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
    private readonly Mock<ILogger<TwitterManager>> _mockLogger = new();

    private TestableTwitterManager CreateSut(Tweet? tweetResult, Exception? exception = null)
        => new(_mockLogger.Object, tweetResult, exception);

    [Fact]
    public async Task PublishAsync_WhenRequestIsValid_ReturnsTweetId()
    {
        var expectedId = "123456789";
        ISocialMediaPublisher sut = CreateSut(new Tweet { ID = expectedId });
        var result = await sut.PublishAsync(new SocialMediaPublishRequest
        {
            Text = "Hello world!",
            ConsumerKey = "key",
            ConsumerSecret = "secret",
            AccessToken = "token",
            AccessTokenSecret = "tokenSecret"
        });
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task PublishAsync_WhenTextIsMissing_ThrowsArgumentException()
    {
        ISocialMediaPublisher sut = CreateSut(new Tweet { ID = "123456789" });
        var act = () => sut.PublishAsync(new SocialMediaPublishRequest { Text = " " });
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task PublishAsync_WhenTweetReturnsNull_ThrowsTwitterPostException()
    {
        ISocialMediaPublisher sut = CreateSut(tweetResult: null);
        var act = () => sut.PublishAsync(new SocialMediaPublishRequest { Text = "Hello world!" });
        await act.Should().ThrowAsync<TwitterPostException>().WithMessage("*Hello world!*");
    }

    [Fact]
    public async Task PublishAsync_WhenExceptionThrown_ThrowsTwitterPostException()
    {
        ISocialMediaPublisher sut = CreateSut(tweetResult: null, exception: new InvalidOperationException("Twitter API error"));
        var act = () => sut.PublishAsync(new SocialMediaPublishRequest { Text = "Hello world!" });
        await act.Should().ThrowAsync<TwitterPostException>().WithMessage("*Hello world!*");
    }

    [Fact]
    public void TwitterPostException_IsA_BroadcastingException()
    {
        new TwitterPostException("test message").Should().BeAssignableTo<BroadcastingException>();
    }

    [Fact]
    public void ITwitterManager_Implements_ISocialMediaPublisher()
    {
        typeof(ISocialMediaPublisher).IsAssignableFrom(typeof(ITwitterManager)).Should().BeTrue();
    }

    [Fact]
    public void ISocialMediaPublisher_DefinesPublishAsync_WithCommonRequestShape()
    {
        var publishAsync = typeof(ISocialMediaPublisher).GetMethod(nameof(ISocialMediaPublisher.PublishAsync));
        publishAsync.Should().NotBeNull();
        publishAsync!.ReturnType.Should().Be(typeof(Task<string?>));
        publishAsync.GetParameters().Should().ContainSingle();
        publishAsync.GetParameters()[0].ParameterType.Should().Be(typeof(SocialMediaPublishRequest));
    }

    private sealed class TestableTwitterManager(
        ILogger<TwitterManager> logger,
        Tweet? tweetResult,
        Exception? exception = null)
        : TwitterManager(logger)
    {
        protected override Task<Tweet?> TweetAsync(
            string consumerKey, string consumerSecret,
            string accessToken, string accessTokenSecret,
            string tweetText)
        {
            if (exception is not null)
                throw exception;
            return Task.FromResult(tweetResult);
        }
    }
}