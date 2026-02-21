using System;
using System.Net.Http;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Managers.Facebook;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Tests;

/// <summary>
/// Unit tests for FacebookManager using Moq to isolate external dependencies
/// </summary>
public class FacebookManagerUnitTests
{
    private readonly Mock<ILogger<FacebookManager>> _mockLogger;
    private readonly Mock<IFacebookApplicationSettings> _mockFacebookSettings;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public FacebookManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<FacebookManager>>();
        _mockFacebookSettings = new Mock<IFacebookApplicationSettings>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        // Setup default settings
        _mockFacebookSettings.Setup(s => s.GraphApiRootUrl).Returns("https://graph.facebook.com");
        _mockFacebookSettings.Setup(s => s.GraphApiVersion).Returns("v21.0");
        _mockFacebookSettings.Setup(s => s.PageId).Returns("testPageId");
        _mockFacebookSettings.Setup(s => s.PageAccessToken).Returns("testAccessToken");
    }

    #region PostMessageAndLinkToPage Tests

    [Fact]
    public async Task PostMessageAndLinkToPage_WithEmptyMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostMessageAndLinkToPage("", "https://example.com"));

        Assert.Equal("message", exception.ParamName);
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_WithEmptyLink_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostMessageAndLinkToPage("Test Message", ""));

        Assert.Equal("link", exception.ParamName);
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_WithEmptyToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.RefreshToken(""));

        Assert.Equal("tokenToRefresh", exception.ParamName);
    }

    #endregion
}
