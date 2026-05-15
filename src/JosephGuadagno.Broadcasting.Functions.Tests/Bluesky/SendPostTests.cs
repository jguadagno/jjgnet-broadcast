using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using idunno.Bluesky.Embed;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Bluesky;

public class SendPostTests
{
    private readonly Mock<IBlueskyManager> _blueskyManager = new();
    private readonly Mock<IUserPublisherSettingManager> _userPublisherSettingManager = new();

    private Functions.Bluesky.SendPost BuildSut() => new(
        _blueskyManager.Object,
        _userPublisherSettingManager.Object,
        NullLogger<Functions.Bluesky.SendPost>.Instance);

    private static BlueskyPostMessage BuildBlueskyPostMessage(
        string text = "Test Bluesky post",
        string? url = null,
        string? shortenedUrl = null,
        List<string>? hashtags = null,
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

    private void SetupValidCredentials(string oid) =>
        _userPublisherSettingManager
            .Setup(m => m.GetCredentialsAsync(oid, SocialMediaPlatformIds.Bluesky, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>
            {
                ["Identifier"] = "user.bsky.social",
                ["AppPassword"] = "app-password"
            });

    // Missing CreatedByEntraOid → skip, no exception

    [Fact]
    public async Task Run_WhenCreatedByEntraOidIsNull_SkipsPostingWithoutException()
    {
        var postMessage = BuildBlueskyPostMessage(); // no OID
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(postMessage));

        Assert.Null(exception);
        _userPublisherSettingManager.Verify(
            m => m.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _blueskyManager.Verify(m => m.GetEmbeddedExternalRecord(It.IsAny<string>()), Times.Never);
    }

    // Missing credentials (empty dictionary) → skip, no exception

    [Fact]
    public async Task Run_WhenCredentialsMissingRequiredKeys_SkipsPostingWithoutException()
    {
        var postMessage = BuildBlueskyPostMessage(createdByEntraOid: "test-oid");
        _userPublisherSettingManager
            .Setup(m => m.GetCredentialsAsync("test-oid", SocialMediaPlatformIds.Bluesky, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>());
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(postMessage));

        Assert.Null(exception);
        _blueskyManager.Verify(m => m.GetEmbeddedExternalRecord(It.IsAny<string>()), Times.Never);
    }

    // Only Identifier present (missing AppPassword) → skip

    [Fact]
    public async Task Run_WhenAppPasswordMissing_SkipsPostingWithoutException()
    {
        var postMessage = BuildBlueskyPostMessage(createdByEntraOid: "test-oid");
        _userPublisherSettingManager
            .Setup(m => m.GetCredentialsAsync("test-oid", SocialMediaPlatformIds.Bluesky, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?> { ["Identifier"] = "user.bsky.social" });
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

