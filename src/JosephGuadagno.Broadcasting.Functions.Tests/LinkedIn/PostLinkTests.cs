using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class PostLinkTests
{
    private readonly Mock<ILinkedInManager> _linkedInManager = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
    private HttpClient _httpClient = null!;

    private Functions.LinkedIn.PostLink BuildSut()
    {
        _httpClient = new HttpClient(_httpMessageHandler.Object);
        return new Functions.LinkedIn.PostLink(
            _linkedInManager.Object,
            _httpClient,
            NullLogger<Functions.LinkedIn.PostLink>.Instance);
    }

    private static LinkedInPostLink BuildLinkedInPostLink(
        string accessToken = "test-access-token",
        string authorId = "urn:li:person:test123",
        string text = "Check this out",
        string linkUrl = "https://example.com/article",
        string title = "Article Title",
        string description = "Article description",
        string? imageUrl = null) => new()
    {
        AccessToken = accessToken,
        AuthorId = authorId,
        Text = text,
        LinkUrl = linkUrl,
        Title = title,
        Description = description,
        ImageUrl = imageUrl
    };

    // ── Successful link post without image ───────────────────────────────────

    [Fact]
    public async Task Run_WithValidLinkWithoutImage_CallsPostShareTextAndLink()
    {
        // Arrange
        var postLink = BuildLinkedInPostLink();
        _linkedInManager
            .Setup(m => m.PostShareTextAndLink(
                postLink.AccessToken, postLink.AuthorId, postLink.Text,
                postLink.LinkUrl, postLink.Title, postLink.Description))
            .ReturnsAsync("share-id-link-123");

        var sut = BuildSut();

        // Act
        await sut.Run(postLink);

        // Assert
        _linkedInManager.Verify(
            m => m.PostShareTextAndLink(
                postLink.AccessToken, postLink.AuthorId, postLink.Text,
                postLink.LinkUrl, postLink.Title, postLink.Description),
            Times.Once);
        _linkedInManager.Verify(
            m => m.PostShareTextAndImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── Successful link post with image (HTTP 200) ───────────────────────────

    [Fact]
    public async Task Run_WithValidLinkWithImage_WhenImageDownloadSucceeds_CallsPostShareTextAndImage()
    {
        // Arrange
        var postLink = BuildLinkedInPostLink(imageUrl: "https://example.com/image.png");
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // fake PNG header

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == postLink.ImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
            });

        _linkedInManager
            .Setup(m => m.PostShareTextAndImage(
                postLink.AccessToken, postLink.AuthorId, postLink.Text,
                It.IsAny<byte[]>(), postLink.Title, postLink.Description))
            .ReturnsAsync("share-id-image-456");

        var sut = BuildSut();

        // Act
        await sut.Run(postLink);

        // Assert
        _linkedInManager.Verify(
            m => m.PostShareTextAndImage(
                postLink.AccessToken, postLink.AuthorId, postLink.Text,
                It.Is<byte[]>(b => b.Length == imageBytes.Length), postLink.Title, postLink.Description),
            Times.Once);
        _linkedInManager.Verify(
            m => m.PostShareTextAndLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── Image download fails (HTTP 404) — fallback to link post ──────────────

    [Fact]
    public async Task Run_WithValidLinkWithImage_WhenImageDownloadFails_FallsBackToLinkPost()
    {
        // Arrange
        var postLink = BuildLinkedInPostLink(imageUrl: "https://example.com/missing.png");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == postLink.ImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        _linkedInManager
            .Setup(m => m.PostShareTextAndLink(
                postLink.AccessToken, postLink.AuthorId, postLink.Text,
                postLink.LinkUrl, postLink.Title, postLink.Description))
            .ReturnsAsync("share-id-fallback-789");

        var sut = BuildSut();

        // Act
        await sut.Run(postLink);

        // Assert — should fallback to link post when image download fails
        _linkedInManager.Verify(
            m => m.PostShareTextAndLink(
                postLink.AccessToken, postLink.AuthorId, postLink.Text,
                postLink.LinkUrl, postLink.Title, postLink.Description),
            Times.Once);
        _linkedInManager.Verify(
            m => m.PostShareTextAndImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── Manager returns null (post failed) ───────────────────────────────────

    [Fact]
    public async Task Run_WhenManagerReturnsNull_DoesNotThrow()
    {
        // Arrange — manager returns null (post failed but no exception)
        var postLink = BuildLinkedInPostLink();
        _linkedInManager
            .Setup(m => m.PostShareTextAndLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var sut = BuildSut();

        // Act & Assert — should not throw
        var exception = await Record.ExceptionAsync(() => sut.Run(postLink));
        Assert.Null(exception);
    }

    // ── LinkedInPostException handling ───────────────────────────────────────

    [Fact]
    public async Task Run_WhenLinkedInPostExceptionThrown_RethrowsException()
    {
        // Arrange
        var postLink = BuildLinkedInPostLink();
        var linkedInException = new LinkedInPostException("API Error", 403, "Forbidden");
        _linkedInManager
            .Setup(m => m.PostShareTextAndLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(linkedInException);

        var sut = BuildSut();

        // Act & Assert — should rethrow LinkedInPostException
        await Assert.ThrowsAsync<LinkedInPostException>(() => sut.Run(postLink));
    }

    // ── Generic exception handling ────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenGenericExceptionThrown_RethrowsException()
    {
        // Arrange
        var postLink = BuildLinkedInPostLink();
        _linkedInManager
            .Setup(m => m.PostShareTextAndLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        var sut = BuildSut();

        // Act & Assert — should rethrow generic Exception
        await Assert.ThrowsAsync<Exception>(() => sut.Run(postLink));
    }
}
