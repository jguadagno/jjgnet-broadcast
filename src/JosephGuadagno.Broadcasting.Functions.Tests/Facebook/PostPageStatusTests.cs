using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Facebook;

public class PostPageStatusTests
{
    private readonly Mock<IFacebookManager> _facebookManager = new();
    private readonly Mock<IUserPublisherSettingManager> _userPublisherSettingManager = new();

    private Functions.Facebook.PostPageStatus BuildSut() => new(
        _facebookManager.Object,
        _userPublisherSettingManager.Object,
        NullLogger<Functions.Facebook.PostPageStatus>.Instance);

    private static FacebookPostStatus BuildFacebookPostStatus(
        string statusText = "Test status",
        string linkUri = "https://example.com",
        string? imageUrl = null,
        string? createdByEntraOid = "test-oid") => new()
    {
        StatusText = statusText,
        LinkUri = linkUri,
        ImageUrl = imageUrl,
        CreatedByEntraOid = createdByEntraOid
    };

    private void SetupValidCredentials(string oid = "test-oid") =>
        _userPublisherSettingManager
            .Setup(m => m.GetCredentialsAsync(oid, SocialMediaPlatformIds.Facebook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>
            {
                ["PageId"] = "page-123",
                ["PageAccessToken"] = "token-abc"
            });

    // Missing CreatedByEntraOid → skip, no exception

    [Fact]
    public async Task Run_WhenCreatedByEntraOidIsNull_SkipsPostingWithoutException()
    {
        var postStatus = BuildFacebookPostStatus(createdByEntraOid: null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(postStatus));

        Assert.Null(exception);
        _userPublisherSettingManager.Verify(
            m => m.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _facebookManager.Verify(
            m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // Missing credentials → skip, no exception

    [Fact]
    public async Task Run_WhenCredentialsMissing_SkipsPostingWithoutException()
    {
        var postStatus = BuildFacebookPostStatus();
        _userPublisherSettingManager
            .Setup(m => m.GetCredentialsAsync("test-oid", SocialMediaPlatformIds.Facebook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>());
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(postStatus));

        Assert.Null(exception);
        _facebookManager.Verify(
            m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // Successful post without image

    [Fact]
    public async Task Run_WithValidStatusWithoutImage_CallsPostMessageAndLink()
    {
        var postStatus = BuildFacebookPostStatus();
        SetupValidCredentials();
        _facebookManager
            .Setup(m => m.PostMessageAndLinkToPage(postStatus.StatusText, postStatus.LinkUri, "page-123", "token-abc"))
            .ReturnsAsync("post-id-123");
        var sut = BuildSut();

        await sut.Run(postStatus);

        _facebookManager.Verify(
            m => m.PostMessageAndLinkToPage(postStatus.StatusText, postStatus.LinkUri, "page-123", "token-abc"),
            Times.Once);
        _facebookManager.Verify(
            m => m.PostMessageLinkAndPictureToPage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // Successful post with image

    [Fact]
    public async Task Run_WithValidStatusWithImage_CallsPostMessageLinkAndPicture()
    {
        var postStatus = BuildFacebookPostStatus(imageUrl: "https://example.com/image.jpg");
        SetupValidCredentials();
        _facebookManager
            .Setup(m => m.PostMessageLinkAndPictureToPage(postStatus.StatusText, postStatus.LinkUri, postStatus.ImageUrl!, "page-123", "token-abc"))
            .ReturnsAsync("post-id-456");
        var sut = BuildSut();

        await sut.Run(postStatus);

        _facebookManager.Verify(
            m => m.PostMessageLinkAndPictureToPage(postStatus.StatusText, postStatus.LinkUri, postStatus.ImageUrl!, "page-123", "token-abc"),
            Times.Once);
        _facebookManager.Verify(
            m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // FacebookPostException is rethrown

    [Fact]
    public async Task Run_WhenFacebookPostExceptionThrown_RethrowsException()
    {
        var postStatus = BuildFacebookPostStatus();
        SetupValidCredentials();
        var facebookException = new FacebookPostException("API Error", 400, "Invalid token");
        _facebookManager
            .Setup(m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(facebookException);
        var sut = BuildSut();

        await Assert.ThrowsAsync<FacebookPostException>(() => sut.Run(postStatus));
    }

    // Generic exception is rethrown

    [Fact]
    public async Task Run_WhenGenericExceptionThrown_RethrowsException()
    {
        var postStatus = BuildFacebookPostStatus();
        SetupValidCredentials();
        _facebookManager
            .Setup(m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network failure"));
        var sut = BuildSut();

        await Assert.ThrowsAsync<Exception>(() => sut.Run(postStatus));
    }
}

