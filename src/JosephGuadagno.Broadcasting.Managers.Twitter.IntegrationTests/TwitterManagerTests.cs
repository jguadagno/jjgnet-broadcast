using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using LinqToTwitter.OAuth;

namespace JosephGuadagno.Broadcasting.Managers.Twitter.IntegrationTests;

[Trait("Category", "Integration")]
public class TwitterManagerTests(ITwitterManager twitterManager, InMemoryCredentialStore credentialStore, TwitterContext twitterContext)
{
    private SocialMediaPublishRequest BuildRequest(string text) => new()
    {
        Text = text,
        ConsumerKey = credentialStore.ConsumerKey,
        ConsumerSecret = credentialStore.ConsumerSecret,
        AccessToken = credentialStore.OAuthToken,
        AccessTokenSecret = credentialStore.OAuthTokenSecret,
    };

    [Fact(Skip = "Manually run only")]
    public async Task DispatchAsync_WithValidTweetText_ReturnsTweetId()
    {
        // Arrange
        var tweetText = $"Integration test tweet [{DateTime.UtcNow:o}]";

        // Act
        var tweetId = await twitterManager.DispatchAsync(BuildRequest(tweetText));

        // Assert
        tweetId.Should().NotBeNullOrEmpty();

        // Cleanup
        if (!string.IsNullOrEmpty(tweetId))
        {
            await twitterContext.DeleteTweetAsync(tweetId);
        }
    }

    [Fact(Skip = "Manually run only")]
    public async Task DispatchAsync_WithEmptyTweetText_ThrowsException()
    {
        // Arrange & Act
        var act = async () => await twitterManager.DispatchAsync(BuildRequest(string.Empty));

        // Assert
        await act.Should().ThrowAsync<TwitterPostException>();
    }

    [Fact(Skip = "Manually run only")]
    public async Task DispatchAsync_WithTweetTextAtMaxLength_ReturnsTweetId()
    {
        // Arrange
        var tweetText = new string('a', 280);

        // Act
        var tweetId = await twitterManager.DispatchAsync(BuildRequest(tweetText));

        // Assert
        tweetId.Should().NotBeNullOrEmpty();

        // Cleanup
        if (!string.IsNullOrEmpty(tweetId))
        {
            await twitterContext.DeleteTweetAsync(tweetId);
        }
    }

    [Fact(Skip = "Manually run only")]
    public async Task DispatchAsync_WithTweetTextExceedingMaxLength_ThrowsException()
    {
        // Arrange & Act
        var act = async () => await twitterManager.DispatchAsync(BuildRequest(new string('a', 281)));

        // Assert
        await act.Should().ThrowAsync<TwitterPostException>();
    }
}
