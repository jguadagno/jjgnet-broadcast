using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;

namespace JosephGuadagno.Broadcasting.Managers.Twitter.IntegrationTests;

[Trait("Category", "Integration")]
public class TwitterManagerTests(ITwitterManager twitterManager, TwitterContext twitterContext)
{
	[Fact(Skip = "Manually run only")]
    public async Task SendTweetAsync_WithValidTweetText_ReturnsTweetId()
    {
        // Arrange
        var tweetText = $"Integration test tweet [{DateTime.UtcNow:o}]";

        // Act
        var tweetId = await twitterManager.SendTweetAsync(tweetText);

        // Assert
        tweetId.Should().NotBeNullOrEmpty();

        // Cleanup
        if (!string.IsNullOrEmpty(tweetId))
        {
            await twitterContext.DeleteTweetAsync(tweetId);
        }
    }

    [Fact(Skip = "Manually run only")]
    public async Task SendTweetAsync_WithEmptyTweetText_ThrowsException()
    {
        // Arrange
        var tweetText = string.Empty;

        // Act
        var act = async () => await twitterManager.SendTweetAsync(tweetText);

        // Assert
        await act.Should().ThrowAsync<TwitterPostException>();
    }

    [Fact(Skip = "Manually run only")]
    public async Task SendTweetAsync_WithTweetTextAtMaxLength_ReturnsTweetId()
    {
        // Arrange
        var tweetText = new string('a', 280);

        // Act
        var tweetId = await twitterManager.SendTweetAsync(tweetText);

        // Assert
        tweetId.Should().NotBeNullOrEmpty();

        // Cleanup
        if (!string.IsNullOrEmpty(tweetId))
        {
            await twitterContext.DeleteTweetAsync(tweetId);
        }
    }

    [Fact(Skip = "Manually run only")]
    public async Task SendTweetAsync_WithTweetTextExceedingMaxLength_ThrowsException()
    {
        // Arrange
        var tweetText = new string('a', 281);

        // Act
        var act = async () => await twitterManager.SendTweetAsync(tweetText);

        // Assert
        await act.Should().ThrowAsync<TwitterPostException>();
    }
}
