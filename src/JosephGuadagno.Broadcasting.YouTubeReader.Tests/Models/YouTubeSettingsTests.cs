using JosephGuadagno.Broadcasting.YouTubeReader.Models;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Tests.Models;

public class YouTubeSettingsTests
{
    [Fact]
    public void ApiKey_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var settings = new YouTubeSettings();
        var expectedValue = "test-api-key";

        // Act
        settings.ApiKey = expectedValue;

        // Assert
        Assert.Equal(expectedValue, settings.ApiKey);
    }

    [Fact]
    public void ChannelId_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var settings = new YouTubeSettings();
        var expectedValue = "test-channel-id";

        // Act
        settings.ChannelId = expectedValue;

        // Assert
        Assert.Equal(expectedValue, settings.ChannelId);
    }

    [Fact]
    public void PlaylistId_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var settings = new YouTubeSettings();
        var expectedValue = "test-playlist-id";

        // Act
        settings.PlaylistId = expectedValue;

        // Assert
        Assert.Equal(expectedValue, settings.PlaylistId);
    }

    [Fact]
    public void ResultSetPageSize_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var settings = new YouTubeSettings();
        var expectedValue = 25;

        // Act
        settings.ResultSetPageSize = expectedValue;

        // Assert
        Assert.Equal(expectedValue, settings.ResultSetPageSize);
    }

    [Fact]
    public void DefaultValues_AllStringsAreNull_IntIsZero()
    {
        // Arrange & Act
        var settings = new YouTubeSettings();

        // Assert
        Assert.Null(settings.ApiKey);
        Assert.Null(settings.ChannelId);
        Assert.Null(settings.PlaylistId);
        Assert.Equal(0, settings.ResultSetPageSize);
    }
}
