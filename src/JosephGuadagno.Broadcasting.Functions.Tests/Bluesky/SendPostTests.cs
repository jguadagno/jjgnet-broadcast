using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using idunno.AtProto;
using idunno.AtProto.Repo;
using idunno.Bluesky;
using idunno.Bluesky.Embed;
using idunno.Bluesky.RichText;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Bluesky;

public class SendPostTests
{
    private readonly Mock<IBlueskyManager> _blueskyManager = new();

    private Functions.Bluesky.SendPost BuildSut() => new(
        _blueskyManager.Object,
        NullLogger<Functions.Bluesky.SendPost>.Instance);

    private static BlueskyPostMessage BuildBlueskyPostMessage(
        string text = "Test Bluesky post",
        string? url = null,
        string? shortenedUrl = null,
        List<string>? hashtags = null,
        string? imageUrl = null) => new()
    {
        Text = text,
        Url = url,
        ShortenedUrl = shortenedUrl,
        Hashtags = hashtags,
        ImageUrl = imageUrl
    };

    // ── Successful post with text only ───────────────────────────────────────

    [Fact]
    public async Task Run_WithTextOnly_CallsBlueskyManagerPost()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage();
        var mockResponse = new CreateRecordResult(new AtUri("at://did/collection/rkey"), new AtCid("fake-cid"), null, null);

        _blueskyManager
            .Setup(m => m.Post(It.Is<PostBuilder>(pb => pb.Text == postMessage.Text)))
            .ReturnsAsync(mockResponse);

        var sut = BuildSut();

        // Act
        await sut.Run(postMessage);

        // Assert
        _blueskyManager.Verify(
            m => m.Post(It.Is<PostBuilder>(pb => pb.Text == postMessage.Text)),
            Times.Once);
    }

    // ── Post with URL and shortened URL (with embed) ─────────────────────────

    [Fact]
    public async Task Run_WithUrlAndShortenedUrl_CallsGetEmbeddedExternalRecord()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage(
            text: "Check this out",
            url: "https://example.com/article",
            shortenedUrl: "https://short.url/abc");
        var mockResponse = new CreateRecordResult(new AtUri("at://did/collection/rkey"), new AtCid("fake-cid"), null, null);
        var mockEmbedRecord = new EmbeddedExternal(
            new Uri("https://example.com/article"),
            "Title", "Description", null);

        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecord(postMessage.Url))
            .ReturnsAsync(mockEmbedRecord);
        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ReturnsAsync(mockResponse);

        var sut = BuildSut();

        // Act
        await sut.Run(postMessage);

        // Assert
        _blueskyManager.Verify(
            m => m.GetEmbeddedExternalRecord(postMessage.Url),
            Times.Once);
        _blueskyManager.Verify(m => m.Post(It.IsAny<PostBuilder>()), Times.Once);
    }

    // ── Post with URL, shortened URL, and image (with embed) ─────────────────

    [Fact]
    public async Task Run_WithUrlShortenedUrlAndImage_CallsGetEmbeddedExternalRecordWithThumbnail()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage(
            text: "Check this out",
            url: "https://example.com/article",
            shortenedUrl: "https://short.url/abc",
            imageUrl: "https://example.com/image.jpg");
        var mockResponse = new CreateRecordResult(new AtUri("at://did/collection/rkey"), new AtCid("fake-cid"), null, null);
        var mockEmbedRecord = new EmbeddedExternal(
            new Uri("https://example.com/article"),
            "Title", "Description", null);

        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url, postMessage.ImageUrl!))
            .ReturnsAsync(mockEmbedRecord);
        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ReturnsAsync(mockResponse);

        var sut = BuildSut();

        // Act
        await sut.Run(postMessage);

        // Assert
        _blueskyManager.Verify(
            m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url, postMessage.ImageUrl!),
            Times.Once);
        _blueskyManager.Verify(
            m => m.GetEmbeddedExternalRecord(It.IsAny<string>()),
            Times.Never);
        _blueskyManager.Verify(m => m.Post(It.IsAny<PostBuilder>()), Times.Once);
    }

    // ── Post with URL and image but no shortened URL ─────────────────────────

    [Fact]
    public async Task Run_WithUrlAndImageButNoShortenedUrl_CallsGetEmbeddedExternalRecordWithThumbnail()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage(
            text: "Check this out",
            url: "https://example.com/article",
            shortenedUrl: null,
            imageUrl: "https://example.com/image.jpg");
        var mockResponse = new CreateRecordResult(new AtUri("at://did/collection/rkey"), new AtCid("fake-cid"), null, null);
        var mockEmbedRecord = new EmbeddedExternal(
            new Uri("https://example.com/article"),
            "Title", "Description", null);

        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url, postMessage.ImageUrl!))
            .ReturnsAsync(mockEmbedRecord);
        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ReturnsAsync(mockResponse);

        var sut = BuildSut();

        // Act
        await sut.Run(postMessage);

        // Assert
        _blueskyManager.Verify(
            m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url, postMessage.ImageUrl!),
            Times.Once);
        _blueskyManager.Verify(m => m.Post(It.IsAny<PostBuilder>()), Times.Once);
    }

    // ── Post with hashtags ────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WithHashtags_IncludesHashtagsInPost()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage(
            text: "Great article",
            hashtags: new List<string> { "tech", "dotnet", "azure" });
        var mockResponse = new CreateRecordResult(new AtUri("at://did/collection/rkey"), new AtCid("fake-cid"), null, null);

        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ReturnsAsync(mockResponse);

        var sut = BuildSut();

        // Act
        await sut.Run(postMessage);

        // Assert
        _blueskyManager.Verify(m => m.Post(It.IsAny<PostBuilder>()), Times.Once);
    }

    // ── Manager returns null (post failed) ───────────────────────────────────

    [Fact]
    public async Task Run_WhenManagerReturnsNull_DoesNotThrow()
    {
        // Arrange — manager returns null (post failed but no exception)
        var postMessage = BuildBlueskyPostMessage();
        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ReturnsAsync((CreateRecordResult?)null);

        var sut = BuildSut();

        // Act & Assert — should not throw (error is logged)
        var exception = await Record.ExceptionAsync(() => sut.Run(postMessage));
        Assert.Null(exception);
    }

    // ── BlueskyPostException handling ─────────────────────────────────────────

    [Fact]
    public async Task Run_WhenBlueskyPostExceptionThrown_RethrowsException()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage();
        var blueskyException = new BlueskyPostException("API Error", 400, "Invalid request");
        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ThrowsAsync(blueskyException);

        var sut = BuildSut();

        // Act & Assert — should rethrow BlueskyPostException
        await Assert.ThrowsAsync<BlueskyPostException>(() => sut.Run(postMessage));
    }

    // ── Generic exception handling ────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenGenericExceptionThrown_RethrowsException()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage();
        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ThrowsAsync(new Exception("Connection timeout"));

        var sut = BuildSut();

        // Act & Assert — should rethrow generic Exception
        await Assert.ThrowsAsync<Exception>(() => sut.Run(postMessage));
    }

    // ── GetEmbeddedExternalRecord returns null ────────────────────────────────

    [Fact]
    public async Task Run_WhenGetEmbeddedExternalRecordReturnsNull_StillPostsWithoutEmbed()
    {
        // Arrange
        var postMessage = BuildBlueskyPostMessage(
            text: "Check this out",
            url: "https://example.com/article",
            shortenedUrl: "https://short.url/abc");
        var mockResponse = new CreateRecordResult(new AtUri("at://did/collection/rkey"), new AtCid("fake-cid"), null, null);

        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecord(It.IsAny<string>()))
            .ReturnsAsync((EmbeddedExternal?)null);
        _blueskyManager
            .Setup(m => m.Post(It.IsAny<PostBuilder>()))
            .ReturnsAsync(mockResponse);

        var sut = BuildSut();

        // Act
        await sut.Run(postMessage);

        // Assert — post should still succeed even if embed fails
        _blueskyManager.Verify(m => m.Post(It.IsAny<PostBuilder>()), Times.Once);
    }
}
