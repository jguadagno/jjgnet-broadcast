using FluentAssertions;

using JosephGuadagno.Broadcasting.JsonFeedReader.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.JsonFeedReader.Tests;

public class JsonFeedReaderTests
{
    // ### Constructor Tests ###

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrowException()
    {
        // Arrange
        var jsonFeedReaderSettings = new JsonFeedReaderSettings
        {
            FeedUrl = "https://josephguadagno.net/feed.json"
        };
        var logger = new Mock<ILogger<JsonFeedReader>>().Object;

        // Act
        var jsonFeedReader = new JsonFeedReader(jsonFeedReaderSettings, logger);

        // Assert
        jsonFeedReader.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullFeedSettings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<JsonFeedReader>>().Object;

        // Act
        Action act = () => new JsonFeedReader(null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*JsonFeedReaderSettings*");
    }

    [Fact]
    public void Constructor_WithFeedSettingsUrlNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<JsonFeedReader>>().Object;
        var jsonFeedReaderSettings = new JsonFeedReaderSettings
        {
            FeedUrl = null!
        };

        // Act
        Action act = () => new JsonFeedReader(jsonFeedReaderSettings, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*FeedUrl*");
    }

    [Fact]
    public void Constructor_WithFeedSettingsUrlEmpty_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<JsonFeedReader>>().Object;
        var jsonFeedReaderSettings = new JsonFeedReaderSettings
        {
            FeedUrl = string.Empty
        };

        // Act
        Action act = () => new JsonFeedReader(jsonFeedReaderSettings, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*FeedUrl*");
    }

    // ### GetSinceDate Tests ###
    // Integration tests for GetSinceDate, GetAsync would go here
    // These would require mocking HttpClient or using a test JSON feed
    // Following the pattern established in SyndicationFeedReader.IntegrationTests,
    // these tests have been intentionally excluded from unit tests to avoid
    // external network dependencies
}
