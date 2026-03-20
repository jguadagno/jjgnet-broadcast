using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Facebook;

public class PostPageStatusTests
{
    private readonly Mock<IFacebookManager> _facebookManager = new();

    private Functions.Facebook.PostPageStatus BuildSut() => new(
        _facebookManager.Object,
        NullLogger<Functions.Facebook.PostPageStatus>.Instance);

    private static FacebookPostStatus BuildFacebookPostStatus(
        string statusText = "Test status",
        string linkUri = "https://example.com",
        string? imageUrl = null) => new()
    {
        StatusText = statusText,
        LinkUri = linkUri,
        ImageUrl = imageUrl
    };

    // Successful post without image

    [Fact]
    public async Task Run_WithValidStatusWithoutImage_CallsPostMessageAndLink()
    {
        // Arrange
        var postStatus = BuildFacebookPostStatus();
        _facebookManager
            .Setup(m => m.PostMessageAndLinkToPage(postStatus.StatusText, postStatus.LinkUri))
            .ReturnsAsync("page-id-123");

        var sut = BuildSut();

        // Act
        await sut.Run(postStatus);

        // Assert
        _facebookManager.Verify(
            m => m.PostMessageAndLinkToPage(postStatus.StatusText, postStatus.LinkUri),
            Times.Once);
        _facebookManager.Verify(
            m => m.PostMessageLinkAndPictureToPage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // Successful post with image

    [Fact]
    public async Task Run_WithValidStatusWithImage_CallsPostMessageLinkAndPicture()
    {
        // Arrange
        var postStatus = BuildFacebookPostStatus(imageUrl: "https://example.com/image.jpg");
        _facebookManager
            .Setup(m => m.PostMessageLinkAndPictureToPage(postStatus.StatusText, postStatus.LinkUri, postStatus.ImageUrl!))
            .ReturnsAsync("page-id-456");

        var sut = BuildSut();

        // Act
        await sut.Run(postStatus);

        // Assert
        _facebookManager.Verify(
            m => m.PostMessageLinkAndPictureToPage(postStatus.StatusText, postStatus.LinkUri, postStatus.ImageUrl!),
            Times.Once);
        _facebookManager.Verify(
            m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // Manager returns null (post failed)

    [Fact]
    public async Task Run_WhenManagerReturnsNull_DoesNotThrow()
    {
        // Arrange - manager returns null (post failed but no exception)
        var postStatus = BuildFacebookPostStatus();
        _facebookManager
            .Setup(m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var sut = BuildSut();

        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(() => sut.Run(postStatus));
        Assert.Null(exception);
    }

    // FacebookPostException handling

    [Fact]
    public async Task Run_WhenFacebookPostExceptionThrown_RethrowsException()
    {
        // Arrange
        var postStatus = BuildFacebookPostStatus();
        var facebookException = new FacebookPostException("API Error", 400, "Invalid token");
        _facebookManager
            .Setup(m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(facebookException);

        var sut = BuildSut();

        // Act & Assert - should rethrow FacebookPostException
        await Assert.ThrowsAsync<FacebookPostException>(() => sut.Run(postStatus));
    }

    // Generic exception handling

    [Fact]
    public async Task Run_WhenGenericExceptionThrown_RethrowsException()
    {
        // Arrange
        var postStatus = BuildFacebookPostStatus();
        _facebookManager
            .Setup(m => m.PostMessageAndLinkToPage(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network failure"));

        var sut = BuildSut();

        // Act & Assert - should rethrow generic Exception
        await Assert.ThrowsAsync<Exception>(() => sut.Run(postStatus));
    }
}
