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
    private readonly Mock<System.Net.Http.HttpMessageHandler> _mockHttpMessageHandler;
    private readonly System.Net.Http.HttpClient _httpClient;

    public FacebookManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<FacebookManager>>();
        _mockFacebookSettings = new Mock<IFacebookApplicationSettings>();
        _mockHttpMessageHandler = new Mock<System.Net.Http.HttpMessageHandler>();
        _httpClient = new System.Net.Http.HttpClient(_mockHttpMessageHandler.Object);

        // Setup default settings
        _mockFacebookSettings.Setup(s => s.GraphApiRootUrl).Returns("https://graph.facebook.com");
        _mockFacebookSettings.Setup(s => s.GraphApiVersion).Returns("v21.0");
        _mockFacebookSettings.Setup(s => s.PageId).Returns("testPageId");
        _mockFacebookSettings.Setup(s => s.PageAccessToken).Returns("testAccessToken");
    }

    #region PostMessageAndLinkToPage Tests

    [Fact]
    public async System.Threading.Tasks.Task PostMessageAndLinkToPage_WithEmptyMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<System.ArgumentNullException>(
            () => sut.PostMessageAndLinkToPage("", "https://example.com"));

        Assert.Equal("message", exception.ParamName);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostMessageAndLinkToPage_WithEmptyLink_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<System.ArgumentNullException>(
            () => sut.PostMessageAndLinkToPage("Test Message", ""));

        Assert.Equal("link", exception.ParamName);
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async System.Threading.Tasks.Task RefreshToken_WithEmptyToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<System.ArgumentNullException>(
            () => sut.RefreshToken(""));

        Assert.Equal("tokenToRefresh", exception.ParamName);
    }

    #endregion
}
