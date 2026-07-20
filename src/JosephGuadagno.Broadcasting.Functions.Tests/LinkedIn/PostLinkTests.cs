using System;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class PostLinkTests
{
    private readonly Mock<ILinkedInManager> _linkedInManager = new();
    private readonly Mock<IUserOAuthTokenManager> _userOAuthTokenManager = new();
    private readonly Mock<IUserPlatformLinkedInSettingsManager> _linkedInSettingsManager = new();

    private Functions.LinkedIn.PostLink BuildSut() => new(
        _linkedInManager.Object,
        _userOAuthTokenManager.Object,
        _linkedInSettingsManager.Object,
        NullLogger<Functions.LinkedIn.PostLink>.Instance);

    private static SocialMediaPublishRequest BuildPublishRequest(
        string text = "Check this out",
        string? linkUrl = "https://example.com/article",
        string? title = "Article Title",
        string? imageUrl = null,
        string? ownerEntraOid = "test-oid") => new()
    {
        Text = text,
        LinkUrl = linkUrl,
        Title = title,
        ImageUrl = imageUrl,
        OwnerEntraOid = ownerEntraOid
    };

    private void SetupValidCredentials(string oid = "test-oid")
    {
        _linkedInSettingsManager
            .Setup(m => m.GetAsync(oid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPlatformLinkedInSettings
            {
                CreatedByEntraOid = oid,
                IsEnabled = true,
                AuthorId = "urn:li:person:test123"
            });
        _userOAuthTokenManager
            .Setup(m => m.GetByUserAndPlatformAsync(oid, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOAuthToken
            {
                CreatedByEntraOid = oid,
                AccessToken = "test-access-token"
            });
    }

    // Missing OwnerEntraOid → skip, no exception

    [Fact]
    public async Task Run_WhenOwnerEntraOidIsNull_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest(ownerEntraOid: null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _linkedInSettingsManager.Verify(
            m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _linkedInManager.Verify(
            m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // Settings not found → skip, no exception

    [Fact]
    public async Task Run_WhenSettingsNotFound_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest();
        _linkedInSettingsManager
            .Setup(m => m.GetAsync("test-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPlatformLinkedInSettings?)null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _linkedInManager.Verify(
            m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // OAuth token not found → skip, no exception

    [Fact]
    public async Task Run_WhenOAuthTokenNotFound_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest();
        _linkedInSettingsManager
            .Setup(m => m.GetAsync("test-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPlatformLinkedInSettings
            {
                CreatedByEntraOid = "test-oid",
                IsEnabled = true,
                AuthorId = "urn:li:person:test123"
            });
        _userOAuthTokenManager
            .Setup(m => m.GetByUserAndPlatformAsync("test-oid", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserOAuthToken?)null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _linkedInManager.Verify(
            m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // Credentials valid → DispatchAsync is called

    [Fact]
    public async Task Run_WhenCredentialsAreValid_CallsDispatchAsync()
    {
        var request = BuildPublishRequest();
        SetupValidCredentials();
        _linkedInManager
            .Setup(m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ReturnsAsync("share-id-123");
        var sut = BuildSut();

        await sut.Run(request);

        _linkedInManager.Verify(
            m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Once);
    }

    // Manager returns null (post failed) → no exception

    [Fact]
    public async Task Run_WhenManagerReturnsNull_DoesNotThrow()
    {
        var request = BuildPublishRequest();
        SetupValidCredentials();
        _linkedInManager
            .Setup(m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ReturnsAsync((string?)null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
    }

    // LinkedInPostException is rethrown

    [Fact]
    public async Task Run_WhenLinkedInPostExceptionThrown_RethrowsException()
    {
        var request = BuildPublishRequest();
        SetupValidCredentials();
        _linkedInManager
            .Setup(m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ThrowsAsync(new LinkedInPostException("API Error", 403, "Forbidden"));
        var sut = BuildSut();

        await Assert.ThrowsAsync<LinkedInPostException>(() => sut.Run(request));
    }

    // Generic exception is rethrown

    [Fact]
    public async Task Run_WhenGenericExceptionThrown_RethrowsException()
    {
        var request = BuildPublishRequest();
        SetupValidCredentials();
        _linkedInManager
            .Setup(m => m.DispatchAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ThrowsAsync(new Exception("Service unavailable"));
        var sut = BuildSut();

        await Assert.ThrowsAsync<Exception>(() => sut.Run(request));
    }
}

