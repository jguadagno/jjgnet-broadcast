using System;
using System.Net.Http;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
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
    private readonly HttpClient _httpClient;
    private readonly Mock<ISocialMediaPlatformManager> _mockSocialMediaPlatformManager;
    private readonly Mock<IMessageTemplateDataStore> _mockMessageTemplateDataStore;
    private readonly Mock<ISyndicationFeedSourceManager> _mockSyndicationFeedSourceManager;
    private readonly Mock<IYouTubeSourceManager> _mockYouTubeSourceManager;
    private readonly Mock<IEngagementManager> _mockEngagementManager;

    public BlueskyManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<BlueskyManager>>();
        _mockBlueskySettings = new Mock<IBlueskySettings>();
        _httpClient = new HttpClient();
        _mockSocialMediaPlatformManager = new Mock<ISocialMediaPlatformManager>();
        _mockMessageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        _mockSyndicationFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        _mockYouTubeSourceManager = new Mock<IYouTubeSourceManager>();
        _mockEngagementManager = new Mock<IEngagementManager>();

        // Setup default settings
        _mockBlueskySettings.Setup(s => s.BlueskyUserName).Returns("testuser");
        _mockBlueskySettings.Setup(s => s.BlueskyPassword).Returns("testpassword");
    }

    private BlueskyManager CreateSut() => new(
        _httpClient,
        _mockBlueskySettings.Object,
        _mockLogger.Object,
        _mockSocialMediaPlatformManager.Object,
        _mockMessageTemplateDataStore.Object,
        _mockSyndicationFeedSourceManager.Object,
        _mockYouTubeSourceManager.Object,
        _mockEngagementManager.Object);

    #region GetEmbeddedExternalRecord Tests

    [Fact]
    public async Task GetEmbeddedExternalRecord_WithEmptyUrl_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEmbeddedExternalRecord("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmbeddedExternalRecord_WithNullUrl_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEmbeddedExternalRecord(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PublishAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ISocialMediaPublisher sut = CreateSut();

        // Act
        var act = () => sut.PublishAsync(null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task PublishAsync_WithBlankText_ThrowsArgumentException()
    {
        // Arrange
        ISocialMediaPublisher sut = CreateSut();

        // Act
        var act = () => sut.PublishAsync(new Domain.Models.SocialMediaPublishRequest { Text = " " });

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public void IBlueskyManager_Implements_ISocialMediaPublisher()
    {
        Assert.True(typeof(ISocialMediaPublisher).IsAssignableFrom(typeof(IBlueskyManager)));
    }

    #endregion

    #region Exception Inheritance Tests

    [Fact]
    public void BlueskyPostException_IsA_BroadcastingException()
    {
        var exception = new BlueskyPostException("test message");

        Assert.IsAssignableFrom<BroadcastingException>(exception);
    }

    #endregion
}