using System.Threading;
using System.Threading.Tasks;

using idunno.Bluesky.Embed;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Bluesky;

public class SendPostTests
{
    private readonly Mock<IBlueskyManager> _blueskyManager = new();
    private readonly Mock<IUserPublisherBlueskySettingsManager> _blueskySettingsManager = new();

    private Functions.Bluesky.SendPost BuildSut() => new(
        _blueskyManager.Object,
        _blueskySettingsManager.Object,
        NullLogger<Functions.Bluesky.SendPost>.Instance);

    private static BlueskyPostMessage BuildBlueskyPostMessage(
        string text = "Test Bluesky post",
        string? url = null,
        string? shortenedUrl = null,
        System.Collections.Generic.List<string>? hashtags = null,
        string? imageUrl = null,
        string? createdByEntraOid = null) => new()
    {
        Text = text,
        Url = url,
        ShortenedUrl = shortenedUrl,
        Hashtags = hashtags,
        ImageUrl = imageUrl,
        CreatedByEntraOid = createdByEntraOid
    };

    private void SetupValidCredentials(string oid)
    {
        _blueskySettingsManager
            .Setup(m => m.GetAsync(oid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPublisherBlueskySettings
            {
                CreatedByEntraOid = oid,
                IsEnabled = true,
                UserName = "user.bsky.social",
                HasAppPassword = true
            });
        _blueskySettingsManager
            .Setup(m => m.GetAppPasswordAsync(oid, It.IsAny<CancellationToken>()))
            .ReturnsAsync("app-password");
    }

    // Missing CreatedByEntraOid → skip, no exception

    [Fact]
    public async Task Run_WhenCreatedByEntraOidIsNull_SkipsPostingWithoutException()
    {
        var postMessage = BuildBlueskyPostMessage(); // no OID
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(postMessage));

        Assert.Null(exception);
        _blueskySettingsManager.Verify(
            m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _blueskyManager.Verify(m => m.GetEmbeddedExternalRecord(It.IsAny<string>()), Times.Never);
    }

    // Settings not found → skip, no exception

    [Fact]
    public async Task Run_WhenSettingsNotFound_SkipsPostingWithoutException()
    {
        var postMessage = BuildBlueskyPostMessage(createdByEntraOid: "test-oid");
        _blueskySettingsManager
            .Setup(m => m.GetAsync("test-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPublisherBlueskySettings?)null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(postMessage));

        Assert.Null(exception);
        _blueskyManager.Verify(m => m.GetEmbeddedExternalRecord(It.IsAny<string>()), Times.Never);
    }

    // App password not found → skip, no exception

    [Fact]
    public async Task Run_WhenAppPasswordMissing_SkipsPostingWithoutException()
    {
        var postMessage = BuildBlueskyPostMessage(createdByEntraOid: "test-oid");
        _blueskySettingsManager
            .Setup(m => m.GetAsync("test-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPublisherBlueskySettings
            {
                CreatedByEntraOid = "test-oid",
                IsEnabled = true,
                UserName = "user.bsky.social"
            });
        _blueskySettingsManager
            .Setup(m => m.GetAppPasswordAsync("test-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(postMessage));

        Assert.Null(exception);
    }

    // GetEmbeddedExternalRecord is called when URL + shortenedUrl are set (before agent login)

    [Fact]
    public async Task Run_WithUrlAndShortenedUrl_CallsGetEmbeddedExternalRecord()
    {
        var postMessage = BuildBlueskyPostMessage(
            text: "Check this out",
            url: "https://example.com/article",
            shortenedUrl: "https://short.url/abc",
            createdByEntraOid: "test-oid");
        SetupValidCredentials("test-oid");
        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecord(postMessage.Url))
            .ReturnsAsync((EmbeddedExternal?)null);
        var sut = BuildSut();

        // agent.Login() will throw a network exception in unit tests — that's expected
        await Record.ExceptionAsync(() => sut.Run(postMessage));

        _blueskyManager.Verify(m => m.GetEmbeddedExternalRecord(postMessage.Url), Times.Once);
    }

    // GetEmbeddedExternalRecordWithThumbnail is called when URL + shortenedUrl + imageUrl are set

    [Fact]
    public async Task Run_WithUrlShortenedUrlAndImage_CallsGetEmbeddedExternalRecordWithThumbnail()
    {
        var postMessage = BuildBlueskyPostMessage(
            text: "Check this out",
            url: "https://example.com/article",
            shortenedUrl: "https://short.url/abc",
            imageUrl: "https://example.com/image.jpg",
            createdByEntraOid: "test-oid");
        SetupValidCredentials("test-oid");
        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url!, postMessage.ImageUrl!))
            .ReturnsAsync((EmbeddedExternal?)null);
        var sut = BuildSut();

        await Record.ExceptionAsync(() => sut.Run(postMessage));

        _blueskyManager.Verify(
            m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url!, postMessage.ImageUrl!),
            Times.Once);
        _blueskyManager.Verify(m => m.GetEmbeddedExternalRecord(It.IsAny<string>()), Times.Never);
    }

    // GetEmbeddedExternalRecordWithThumbnail is called when URL + imageUrl but no shortenedUrl

    [Fact]
    public async Task Run_WithUrlAndImageButNoShortenedUrl_CallsGetEmbeddedExternalRecordWithThumbnail()
    {
        var postMessage = BuildBlueskyPostMessage(
            text: "Check this out",
            url: "https://example.com/article",
            imageUrl: "https://example.com/image.jpg",
            createdByEntraOid: "test-oid");
        SetupValidCredentials("test-oid");
        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url!, postMessage.ImageUrl!))
            .ReturnsAsync((EmbeddedExternal?)null);
        var sut = BuildSut();

        await Record.ExceptionAsync(() => sut.Run(postMessage));

        _blueskyManager.Verify(
            m => m.GetEmbeddedExternalRecordWithThumbnail(postMessage.Url!, postMessage.ImageUrl!),
            Times.Once);
    }

    // BlueskyPostException is rethrown

    [Fact]
    public async Task Run_WhenBlueskyPostExceptionThrown_RethrowsException()
    {
        var postMessage = BuildBlueskyPostMessage(
            url: "https://example.com/article",
            shortenedUrl: "https://short.url/abc",
            createdByEntraOid: "test-oid");
        SetupValidCredentials("test-oid");
        _blueskyManager
            .Setup(m => m.GetEmbeddedExternalRecord(postMessage.Url))
            .ThrowsAsync(new BlueskyPostException("API Error", 400, "Invalid request"));
        var sut = BuildSut();

        // BlueskyPostException from manager (e.g. during OG embed) is rethrown
        await Assert.ThrowsAsync<BlueskyPostException>(() => sut.Run(postMessage));
    }
}
