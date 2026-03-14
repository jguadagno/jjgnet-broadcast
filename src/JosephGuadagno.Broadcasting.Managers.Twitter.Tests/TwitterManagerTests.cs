using FluentAssertions;
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
    public async Task SendTweetAsync_WhenTweetReturnsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut(_mockLogger.Object, tweetResult: null);

        // Act
        var result = await sut.SendTweetAsync("Hello world!");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SendTweetAsync_WhenExceptionThrown_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut(_mockLogger.Object, tweetResult: null, exception: new InvalidOperationException("Twitter API error"));

        // Act
        var result = await sut.SendTweetAsync("Hello world!");

        // Assert
        result.Should().BeNull();
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
