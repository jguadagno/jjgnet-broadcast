using JosephGuadagno.Broadcasting.YouTubeReader.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.YouTubeReader.IntegrationTests;

[Trait("Category", "Integration")]
public class YouTubeReaderTests
{
    private Mock<ILogger<YouTubeReader>> CreateMockLogger()
    {
        return new Mock<ILogger<YouTubeReader>>();
    }

    [Fact]
    public void GetAsync_WhenCalled_ReturnsVideosSinceDate()
    {
        // This test requires a real YouTube API key and should be run as an integration test
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "real-api-key",
            ChannelId = "real-channel-id",
            PlaylistId = "real-playlist-id",
            ResultSetPageSize = 10
        };
        var logger = CreateMockLogger();
        var reader = new YouTubeReader(settings, logger.Object);
        var sinceWhen = DateTimeOffset.UtcNow.AddDays(-30);

        // Act
        var result = reader.GetAsync(sinceWhen).Result;

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GetSinceDate_WhenCalled_ReturnsVideosSinceDate()
    {
        // This test requires a real YouTube API key and should be run as an integration test
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "real-api-key",
            ChannelId = "real-channel-id",
            PlaylistId = "real-playlist-id",
            ResultSetPageSize = 10
        };
        var logger = CreateMockLogger();
        var reader = new YouTubeReader(settings, logger.Object);
        var sinceWhen = DateTimeOffset.UtcNow.AddDays(-30);

        // Act
        var result = reader.GetSinceDate(sinceWhen);

        // Assert
        Assert.NotNull(result);
    }
}
