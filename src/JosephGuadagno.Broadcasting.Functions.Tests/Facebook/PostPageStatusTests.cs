using System;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Facebook;

public class PostPageStatusTests
{
    private readonly Mock<IFacebookManager> _facebookManager = new();
    private readonly Mock<IUserPublisherFacebookSettingsManager> _facebookSettingsManager = new();

    private Functions.Facebook.PostPageStatus BuildSut() => new(
        _facebookManager.Object,
        _facebookSettingsManager.Object,
        NullLogger<Functions.Facebook.PostPageStatus>.Instance);

    private static SocialMediaPublishRequest BuildPublishRequest(
        string text = "Test status",
        string? linkUrl = "https://example.com",
        string? imageUrl = null,
        string? ownerEntraOid = "test-oid") => new()
    {
        Text = text,
        LinkUrl = linkUrl,
        ImageUrl = imageUrl,
        OwnerEntraOid = ownerEntraOid
    };

    private void SetupValidCredentials(string oid = "test-oid")
    {
        _facebookSettingsManager
            .Setup(m => m.GetAsync(oid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPublisherFacebookSettings
            {
                CreatedByEntraOid = oid,
                IsEnabled = true,
                PageId = "page-123",
                HasPageAccessToken = true
            });
        _facebookSettingsManager
            .Setup(m => m.GetPageAccessTokenAsync(oid, It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-abc");
    }

    // Missing OwnerEntraOid → skip, no exception

    [Fact]
    public async Task Run_WhenCreatedByEntraOidIsNull_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest(ownerEntraOid: null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _facebookSettingsManager.Verify(
            m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _facebookManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // Settings not found → skip, no exception

    [Fact]
    public async Task Run_WhenSettingsNotFound_SkipsPostingWithoutException()
    {
        var request = BuildPublishRequest();
        _facebookSettingsManager
            .Setup(m => m.GetAsync("test-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPublisherFacebookSettings?)null);
        var sut = BuildSut();

        var exception = await Record.ExceptionAsync(() => sut.Run(request));

        Assert.Null(exception);
        _facebookManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Never);
    }

    // Successful post → PublishAsync is called

    [Fact]
    public async Task Run_WithValidStatus_CallsPublishAsync()
    {
        var request = BuildPublishRequest();
        SetupValidCredentials();
        _facebookManager
            .Setup(m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ReturnsAsync("post-id-123");
        var sut = BuildSut();

        await sut.Run(request);

        _facebookManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Once);
    }

    // Successful post with image → PublishAsync is called

    [Fact]
    public async Task Run_WithValidStatusWithImage_CallsPublishAsync()
    {
        var request = BuildPublishRequest(imageUrl: "https://example.com/image.jpg");
        SetupValidCredentials();
        _facebookManager
            .Setup(m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ReturnsAsync("post-id-456");
        var sut = BuildSut();

        await sut.Run(request);

        _facebookManager.Verify(
            m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()),
            Times.Once);
    }

    // FacebookPostException is rethrown

    [Fact]
    public async Task Run_WhenFacebookPostExceptionThrown_RethrowsException()
    {
        var request = BuildPublishRequest();
        SetupValidCredentials();
        _facebookManager
            .Setup(m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ThrowsAsync(new FacebookPostException("API Error", 400, "Invalid token"));
        var sut = BuildSut();

        await Assert.ThrowsAsync<FacebookPostException>(() => sut.Run(request));
    }

    // Generic exception is rethrown

    [Fact]
    public async Task Run_WhenGenericExceptionThrown_RethrowsException()
    {
        var request = BuildPublishRequest();
        SetupValidCredentials();
        _facebookManager
            .Setup(m => m.PublishAsync(It.IsAny<SocialMediaPublishRequest>()))
            .ThrowsAsync(new Exception("Network failure"));
        var sut = BuildSut();

        await Assert.ThrowsAsync<Exception>(() => sut.Run(request));
    }
}
