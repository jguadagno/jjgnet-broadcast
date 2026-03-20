using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class PostImageTests
{
    private readonly Mock<ILinkedInManager> _linkedInManager = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
    private HttpClient _httpClient = null!;

    private Functions.LinkedIn.PostImage BuildSut()
    {
        _httpClient = new HttpClient(_httpMessageHandler.Object);
        return new Functions.LinkedIn.PostImage(
            _linkedInManager.Object,
            _httpClient,
            NullLogger<Functions.LinkedIn.PostImage>.Instance);
    }

    private static LinkedInPostImage BuildLinkedInPostImage(
        string accessToken = "test-access-token",
        string authorId = "urn:li:person:test123",
        string text = "Check out this image",
        string imageUrl = "https://example.com/image.jpg",
        string title = "Image Title",
        string description = "Image description") => new()
    {
        AccessToken = accessToken,
        AuthorId = authorId,
        Text = text,
        ImageUrl = imageUrl,
        Title = title,
        Description = description
    };

    // ΓöÇΓöÇ Successful image post (HTTP 200) ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WithValidImage_WhenImageDownloadSucceeds_CallsPostShareTextAndImage()
    {
        // Arrange
        var postImage = BuildLinkedInPostImage();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // fake PNG header

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == postImage.ImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        _linkedInManager
            .Setup(m => m.PostShareTextAndImage(
                postImage.AccessToken, postImage.AuthorId, postImage.Text,
                It.IsAny<byte[]>(), postImage.Title, postImage.Description))
            .ReturnsAsync("share-id-image-999");

        var sut = BuildSut();

        // Act
        await sut.Run(postImage);

        // Assert
        _linkedInManager.Verify(
            m => m.PostShareTextAndImage(
                postImage.AccessToken, postImage.AuthorId, postImage.Text,
                It.Is<byte[]>(b => b.Length == imageBytes.Length), postImage.Title, postImage.Description),
            Times.Once);
    }

    // ΓöÇΓöÇ Image download fails (HTTP 404) ΓÇö no post, no exception ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenImageDownloadFails_DoesNotCallManager()
    {
        // Arrange
        var postImage = BuildLinkedInPostImage();

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == postImage.ImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var sut = BuildSut();

        // Act
        await sut.Run(postImage);

        // Assert ΓÇö manager should NOT be called when image download fails
        _linkedInManager.Verify(
            m => m.PostShareTextAndImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ΓöÇΓöÇ Image download fails (HTTP 500) ΓÇö no post, no exception ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenImageDownloadReturnsServerError_DoesNotCallManager()
    {
        // Arrange
        var postImage = BuildLinkedInPostImage();

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == postImage.ImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var sut = BuildSut();

        // Act
        await sut.Run(postImage);

        // Assert ΓÇö manager should NOT be called when image download fails
        _linkedInManager.Verify(
            m => m.PostShareTextAndImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ΓöÇΓöÇ Manager returns null (post failed) ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenManagerReturnsNull_DoesNotThrow()
    {
        // Arrange ΓÇö manager returns null (post failed but no exception)
        var postImage = BuildLinkedInPostImage();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        _linkedInManager
            .Setup(m => m.PostShareTextAndImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var sut = BuildSut();

        // Act & Assert ΓÇö should not throw
        var exception = await Record.ExceptionAsync(() => sut.Run(postImage));
        Assert.Null(exception);
    }

    // ΓöÇΓöÇ Exception during processing ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenExceptionThrown_DoesNotThrow()
    {
        // Arrange ΓÇö HttpClient throws exception
        var postImage = BuildLinkedInPostImage();
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var sut = BuildSut();

        // Act & Assert ΓÇö exceptions are caught and logged, not rethrown
        var exception = await Record.ExceptionAsync(() => sut.Run(postImage));
        Assert.Null(exception);
    }

    // ΓöÇΓöÇ Manager throws exception ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenManagerThrowsException_DoesNotThrow()
    {
        // Arrange
        var postImage = BuildLinkedInPostImage();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        _linkedInManager
            .Setup(m => m.PostShareTextAndImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("API Error"));

        var sut = BuildSut();

        // Act & Assert ΓÇö exceptions are caught and logged, not rethrown
        var exception = await Record.ExceptionAsync(() => sut.Run(postImage));
        Assert.Null(exception);
    }
}
