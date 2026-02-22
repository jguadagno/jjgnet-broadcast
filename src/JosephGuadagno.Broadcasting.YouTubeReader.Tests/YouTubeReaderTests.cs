using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Tests;

public class YouTubeReaderTests
{
    private Mock<ILogger<YouTubeReader>> CreateMockLogger()
    {
        return new Mock<ILogger<YouTubeReader>>();
    }

    private Mock<IYouTubeSettings> CreateValidSettingsMock()
    {
        var mockSettings = new Mock<IYouTubeSettings>();
        mockSettings.Setup(s => s.ApiKey).Returns("test-api-key");
        mockSettings.Setup(s => s.ChannelId).Returns("test-channel-id");
        mockSettings.Setup(s => s.PlaylistId).Returns("test-playlist-id");
        mockSettings.Setup(s => s.ResultSetPageSize).Returns(10);
        return mockSettings;
    }

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = CreateMockLogger();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new YouTubeReader(null!, logger.Object));
        Assert.Equal("youTubeSettings", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSettings = new Mock<IYouTubeSettings>();
        mockSettings.Setup(s => s.ApiKey).Returns((string)null!);
        mockSettings.Setup(s => s.ChannelId).Returns("test-channel-id");
        mockSettings.Setup(s => s.PlaylistId).Returns("test-playlist-id");
        var logger = CreateMockLogger();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new YouTubeReader(mockSettings.Object, logger.Object));
        Assert.Equal("ApiKey", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSettings = new Mock<IYouTubeSettings>();
        mockSettings.Setup(s => s.ApiKey).Returns(string.Empty);
        mockSettings.Setup(s => s.ChannelId).Returns("test-channel-id");
        mockSettings.Setup(s => s.PlaylistId).Returns("test-playlist-id");
        var logger = CreateMockLogger();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new YouTubeReader(mockSettings.Object, logger.Object));
        Assert.Equal("ApiKey", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullChannelId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSettings = new Mock<IYouTubeSettings>();
        mockSettings.Setup(s => s.ApiKey).Returns("test-api-key");
        mockSettings.Setup(s => s.ChannelId).Returns((string)null!);
        mockSettings.Setup(s => s.PlaylistId).Returns("test-playlist-id");
        var logger = CreateMockLogger();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new YouTubeReader(mockSettings.Object, logger.Object));
        Assert.Equal("ChannelId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyChannelId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSettings = new Mock<IYouTubeSettings>();
        mockSettings.Setup(s => s.ApiKey).Returns("test-api-key");
        mockSettings.Setup(s => s.ChannelId).Returns(string.Empty);
        mockSettings.Setup(s => s.PlaylistId).Returns("test-playlist-id");
        var logger = CreateMockLogger();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new YouTubeReader(mockSettings.Object, logger.Object));
        Assert.Equal("ChannelId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullPlaylistId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSettings = new Mock<IYouTubeSettings>();
        mockSettings.Setup(s => s.ApiKey).Returns("test-api-key");
        mockSettings.Setup(s => s.ChannelId).Returns("test-channel-id");
        mockSettings.Setup(s => s.PlaylistId).Returns((string)null!);
        var logger = CreateMockLogger();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new YouTubeReader(mockSettings.Object, logger.Object));
        Assert.Equal("PlaylistId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyPlaylistId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSettings = new Mock<IYouTubeSettings>();
        mockSettings.Setup(s => s.ApiKey).Returns("test-api-key");
        mockSettings.Setup(s => s.ChannelId).Returns("test-channel-id");
        mockSettings.Setup(s => s.PlaylistId).Returns(string.Empty);
        var logger = CreateMockLogger();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new YouTubeReader(mockSettings.Object, logger.Object));
        Assert.Equal("PlaylistId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidSettings_CreatesInstance()
    {
        // Arrange
        var mockSettings = CreateValidSettingsMock();
        var logger = CreateMockLogger();

        // Act
        var reader = new YouTubeReader(mockSettings.Object, logger.Object);

        // Assert
        Assert.NotNull(reader);
    }

    #endregion

    #region ResultSetPageSize Default Tests

    [Fact]
    public void Constructor_WithZeroPageSize_DefaultsToTen()
    {
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "test-api-key",
            ChannelId = "test-channel-id",
            PlaylistId = "test-playlist-id",
            ResultSetPageSize = 0
        };
        var logger = CreateMockLogger();

        // Act
        var reader = new YouTubeReader(settings, logger.Object);

        // Assert
        Assert.Equal(10, settings.ResultSetPageSize);
    }

    [Fact]
    public void Constructor_WithNegativePageSize_DefaultsToTen()
    {
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "test-api-key",
            ChannelId = "test-channel-id",
            PlaylistId = "test-playlist-id",
            ResultSetPageSize = -1
        };
        var logger = CreateMockLogger();

        // Act
        var reader = new YouTubeReader(settings, logger.Object);

        // Assert
        Assert.Equal(10, settings.ResultSetPageSize);
    }

    [Fact]
    public void Constructor_WithPageSizeOver50_DefaultsToTen()
    {
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "test-api-key",
            ChannelId = "test-channel-id",
            PlaylistId = "test-playlist-id",
            ResultSetPageSize = 51
        };
        var logger = CreateMockLogger();

        // Act
        var reader = new YouTubeReader(settings, logger.Object);

        // Assert
        Assert.Equal(10, settings.ResultSetPageSize);
    }

    [Fact]
    public void Constructor_WithValidPageSize_KeepsValue()
    {
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "test-api-key",
            ChannelId = "test-channel-id",
            PlaylistId = "test-playlist-id",
            ResultSetPageSize = 25
        };
        var logger = CreateMockLogger();

        // Act
        var reader = new YouTubeReader(settings, logger.Object);

        // Assert
        Assert.Equal(25, settings.ResultSetPageSize);
    }

    [Fact]
    public void Constructor_WithPageSizeOf1_KeepsValue()
    {
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "test-api-key",
            ChannelId = "test-channel-id",
            PlaylistId = "test-playlist-id",
            ResultSetPageSize = 1
        };
        var logger = CreateMockLogger();

        // Act
        var reader = new YouTubeReader(settings, logger.Object);

        // Assert
        Assert.Equal(1, settings.ResultSetPageSize);
    }

    [Fact]
    public void Constructor_WithPageSizeOf50_KeepsValue()
    {
        // Arrange
        var settings = new YouTubeSettings
        {
            ApiKey = "test-api-key",
            ChannelId = "test-channel-id",
            PlaylistId = "test-playlist-id",
            ResultSetPageSize = 50
        };
        var logger = CreateMockLogger();

        // Act
        var reader = new YouTubeReader(settings, logger.Object);

        // Assert
        Assert.Equal(50, settings.ResultSetPageSize);
    }

    #endregion

    #region Integration Tests (Skipped)

    [Fact(Skip = "Integration test - requires YouTube API key")]
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

    [Fact(Skip = "Integration test - requires YouTube API key")]
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

    #endregion
}
