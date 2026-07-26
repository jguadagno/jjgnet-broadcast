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

    public BlueskyManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<BlueskyManager>>();
        _mockBlueskySettings = new Mock<IBlueskySettings>();
        _httpClient = new HttpClient();

        // Setup default settings
        _mockBlueskySettings.Setup(s => s.BlueskyUserName).Returns("testuser");
        _mockBlueskySettings.Setup(s => s.BlueskyPassword).Returns("testpassword");
    }

    private BlueskyManager CreateSut() => new(
        _httpClient,
        _mockBlueskySettings.Object,
        _mockLogger.Object);

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
    public async Task DispatchAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ISocialMediaDispatcher sut = CreateSut();

        // Act
        var act = () => sut.DispatchAsync(null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DispatchAsync_WithBlankText_ThrowsArgumentException()
    {
        // Arrange
        ISocialMediaDispatcher sut = CreateSut();

        // Act
        var act = () => sut.DispatchAsync(new Domain.Models.SocialMediaPublishRequest { Text = " " });

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public void IBlueskyManager_Implements_ISocialMediaDispatcher()
    {
        Assert.True(typeof(ISocialMediaDispatcher).IsAssignableFrom(typeof(IBlueskyManager)));
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