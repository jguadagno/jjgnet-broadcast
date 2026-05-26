using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
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

    private static SocialMediaPublishRequest BuildPublishRequest(
        string text = "Test Bluesky post",
        string? linkUrl = null,
        string? shortenedUrl = null,
        IReadOnlyCollection<string>? hashtags = null,
        string? imageUrl = null,
        string? ownerEntraOid = null) => new()
    {
        Text = text,
        LinkUrl = linkUrl,
        ShortenedUrl = shortenedUrl,
        Hashtags = hashtags,
        ImageUrl = imageUrl,
        OwnerEntraOid = ownerEntraOid
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

    // Missing OwnerEntraOid → skip, no exception

    [Fact]
    public async Task Run_WhenCreatedByEntraOidIsNull_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest(); // no OID
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _blueskySettingsManager.Verify(
            m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _blueskyManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // Settings not found → skip, no exception

    [Fact]
    public async Task Run_WhenSettingsNotFound_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest(ownerEntraOid: "test-oid");
        _blueskySettingsManager
            .Setup(m => m.GetAsync("test-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPublisherBlueskySettings?)null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _blueskyManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // App password not found → skip, no exception

    [Fact]
    public async Task Run_WhenAppPasswordMissing_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest(ownerEntraOid: "test-oid");
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

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _blueskyManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // Credentials valid → PublishAsync is called

    [Fact]
    public async Task Run_WhenCredentialsAreValid_CallsPublishAsync()
    {
        var request = BuildPublishRequest(ownerEntraOid: "test-oid");
        SetupValidCredentials("test-oid");
        _blueskyManager
            .Setup(m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ReturnsAsync("cid-abc123");
        var sut = BuildSut();

        await sut.Run(request);

        _blueskyManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Once);
    }

    // BlueskyPostException is rethrown

    [Fact]
    public async Task Run_WhenBlueskyPostExceptionThrown_RethrowsException()
    {
        var request = BuildPublishRequest(
            linkUrl: "https://example.com/article",
            shortenedUrl: "https://short.url/abc",
            ownerEntraOid: "test-oid");
        SetupValidCredentials("test-oid");
        _blueskyManager
            .Setup(m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ThrowsAsync(new BlueskyPostException("API Error", 400, "Invalid request"));
        var sut = BuildSut();

        await Assert.ThrowsAsync<BlueskyPostException>(() => sut.Run(request));
    }
}
