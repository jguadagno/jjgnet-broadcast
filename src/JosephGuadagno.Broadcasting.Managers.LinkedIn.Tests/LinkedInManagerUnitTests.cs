using System.Net;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests;

/// <summary>
/// Unit tests for LinkedInManager using Moq to isolate external dependencies
/// </summary>
public class LinkedInManagerUnitTests
{
    private readonly Mock<ILogger<LinkedInManager>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public LinkedInManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<LinkedInManager>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }

    #region PostShareText Tests

    [Fact]
    public async Task PostShareText_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareText("", "authorId123", "Sample Post Text"));

        Assert.Equal("accessToken", exception.ParamName);
    }

    [Fact]
    public async Task PostShareText_WithEmptyAuthorId_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareText("validAccessToken", "", "Sample Post Text"));

        Assert.Equal("authorId", exception.ParamName);
    }

    [Fact]
    public async Task PostShareText_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareText("validAccessToken", "authorId123", ""));

        Assert.Equal("postText", exception.ParamName);
    }

    #endregion

    #region PostShareTextAndLink Tests

    [Fact]
    public async Task PostShareTextAndLink_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareTextAndLink("", "authorId123", "Sample Post Text", "https://example.com"));

        Assert.Equal("accessToken", exception.ParamName);
    }

    [Fact]
    public async Task PostShareTextAndLink_WithEmptyAuthorId_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareTextAndLink("validAccessToken", "", "Sample Post Text", "https://example.com"));

        Assert.Equal("authorId", exception.ParamName);
    }

    [Fact]
    public async Task PostShareTextAndLink_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareTextAndLink("validAccessToken", "authorId123", "", "https://example.com"));

        Assert.Equal("postText", exception.ParamName);
    }

    [Fact]
    public async Task PostShareTextAndLink_WithEmptyLink_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareTextAndLink("validAccessToken", "authorId123", "Sample Post Text", ""));

        Assert.Equal("link", exception.ParamName);
    }

    #endregion

    #region PostShareTextAndImage Tests

    [Fact]
    public async Task PostShareTextAndImage_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);
        var imageBytes = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareTextAndImage("", "authorId123", "Sample Post Text", imageBytes));

        Assert.Equal("accessToken", exception.ParamName);
    }

    [Fact]
    public async Task PostShareTextAndImage_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);
        var imageBytes = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareTextAndImage("validAccessToken", "authorId123", "", imageBytes));

        Assert.Equal("postText", exception.ParamName);
    }

    [Fact]
    public async Task PostShareTextAndImage_WithEmptyImage_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostShareTextAndImage("validAccessToken", "authorId123", "Sample Post", Array.Empty<byte>()));

        Assert.Equal("image", exception.ParamName);
    }

    #endregion

    #region GetMyLinkedInUserProfile Tests

    [Fact]
    public async Task GetMyLinkedInUserProfile_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.GetMyLinkedInUserProfile(""));

        Assert.Equal("accessToken", exception.ParamName);
    }

    #endregion

    #region Exception Scenario Tests

    [Fact]
    public async Task PostShareText_OnApiFailure_ThrowsLinkedInPostException()
    {
        // Arrange
        var errorJson = "{\"message\": \"Unauthorized\", \"serviceErrorCode\": 401, \"status\": 401}";
        SetupHttpMessageHandler(HttpStatusCode.OK, errorJson);
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LinkedInPostException>(
            () => sut.PostShareText("validToken", "authorId123", "Hello LinkedIn!"));

        Assert.Contains("LinkedIn", exception.Message);
    }

    [Fact]
    public async Task PostShareText_OnApiFailure_PopulatesApiErrorCodeAndMessage()
    {
        // Arrange — LinkedIn returns a failure response (no "id" field, so IsSuccess == false)
        var errorJson = "{\"message\": \"Unauthorized\", \"serviceErrorCode\": 401, \"status\": 401}";
        SetupHttpMessageHandler(HttpStatusCode.OK, errorJson);
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act
        var exception = await Assert.ThrowsAsync<LinkedInPostException>(
            () => sut.PostShareText("validToken", "authorId123", "Hello LinkedIn!"));

        // Assert — structured fields must be populated so PostText's catch handler can log them
        Assert.Equal(401, exception.ApiErrorCode);
        Assert.Equal("Unauthorized", exception.ApiErrorMessage);
    }

    [Fact]
    public async Task PostShareTextAndLink_OnApiFailure_ThrowsLinkedInPostException()
    {
        // Arrange
        var errorJson = "{\"message\": \"Forbidden\", \"serviceErrorCode\": 403, \"status\": 403}";
        SetupHttpMessageHandler(HttpStatusCode.OK, errorJson);
        var sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LinkedInPostException>(
            () => sut.PostShareTextAndLink("validToken", "authorId123", "Hello LinkedIn!", "https://example.com"));

        Assert.Contains("LinkedIn", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithImageUrlDownloadFailure_FallsBackToLinkShare()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(string.Empty)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\": \"share-123\"}", System.Text.Encoding.UTF8, "application/json")
            });

        ISocialMediaPublisher sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act
        var result = await sut.PublishAsync(new SocialMediaPublishRequest
        {
            AccessToken = "validToken",
            AuthorId = "authorId123",
            Text = "Hello LinkedIn!",
            LinkUrl = "https://example.com",
            ImageUrl = "https://example.com/image.png",
            Title = "Title",
            Description = "Description"
        });

        // Assert
        Assert.Equal("share-123", result);
    }

    [Fact]
    public async Task PublishAsync_WithoutAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        ISocialMediaPublisher sut = new LinkedInManager(_httpClient, _mockLogger.Object);

        // Act
        var act = () => sut.PublishAsync(new SocialMediaPublishRequest
        {
            AuthorId = "authorId123",
            Text = "Hello LinkedIn!"
        });

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public void ILinkedInManager_Implements_ISocialMediaPublisher()
    {
        Assert.True(typeof(ISocialMediaPublisher).IsAssignableFrom(typeof(ILinkedInManager)));
    }

    [Fact]
    public void LinkedInPostException_IsA_BroadcastingException()
    {
        var exception = new LinkedInPostException("test message");

        Assert.IsAssignableFrom<BroadcastingException>(exception);
    }

    #endregion

    private void SetupHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<System.Threading.CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
