using JosephGuadagno.Broadcasting.Managers.Bluesky;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Tests;

/// <summary>
/// Unit tests for BlueskyManager using Moq to isolate external dependencies
/// </summary>
public class BlueskyManagerUnitTests
{
    private readonly Mock<ILogger<BlueskyManager>> _mockLogger;
    private readonly Mock<IBlueskySettings> _mockBlueskySettings;
    private readonly System.Net.Http.HttpClient _httpClient;

    public BlueskyManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<BlueskyManager>>();
        _mockBlueskySettings = new Mock<IBlueskySettings>();
        _httpClient = new System.Net.Http.HttpClient();

        // Setup default settings
        _mockBlueskySettings.Setup(s => s.BlueskyUserName).Returns("testuser");
        _mockBlueskySettings.Setup(s => s.BlueskyPassword).Returns("testpassword");
    }

    #region GetEmbeddedExternalRecord Tests

    [Fact]
    public async System.Threading.Tasks.Task GetEmbeddedExternalRecord_WithEmptyUrl_ReturnsNull()
    {
        // Arrange
        var sut = new BlueskyManager(_httpClient, _mockBlueskySettings.Object, _mockLogger.Object);

        // Act
        var result = await sut.GetEmbeddedExternalRecord("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEmbeddedExternalRecord_WithNullUrl_ReturnsNull()
    {
        // Arrange
        var sut = new BlueskyManager(_httpClient, _mockBlueskySettings.Object, _mockLogger.Object);

        // Act
        var result = await sut.GetEmbeddedExternalRecord(null);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
