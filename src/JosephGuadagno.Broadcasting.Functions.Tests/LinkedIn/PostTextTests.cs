using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class PostTextTests
{
    private readonly Mock<ILinkedInManager> _linkedInManager = new();

    private Functions.LinkedIn.PostText BuildSut() => new(
        _linkedInManager.Object,
        NullLogger<Functions.LinkedIn.PostText>.Instance);

    private static LinkedInPostText BuildLinkedInPostText(
        string accessToken = "test-access-token",
        string authorId = "urn:li:person:test123",
        string text = "Test LinkedIn post") => new()
    {
        AccessToken = accessToken,
        AuthorId = authorId,
        Text = text
    };

    // ΓöÇΓöÇ Successful post ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WithValidPostText_CallsPostShareText()
    {
        // Arrange
        var postText = BuildLinkedInPostText();
        _linkedInManager
            .Setup(m => m.PostShareText(postText.AccessToken, postText.AuthorId, postText.Text))
            .ReturnsAsync("share-id-789");

        var sut = BuildSut();

        // Act
        await sut.Run(postText);

        // Assert
        _linkedInManager.Verify(
            m => m.PostShareText(postText.AccessToken, postText.AuthorId, postText.Text),
            Times.Once);
    }

    // ΓöÇΓöÇ Manager returns null (post failed) ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenManagerReturnsNull_DoesNotThrow()
    {
        // Arrange ΓÇö manager returns null (post failed but no exception)
        var postText = BuildLinkedInPostText();
        _linkedInManager
            .Setup(m => m.PostShareText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var sut = BuildSut();

        // Act & Assert ΓÇö should not throw
        var exception = await Record.ExceptionAsync(() => sut.Run(postText));
        Assert.Null(exception);
    }

    // ΓöÇΓöÇ LinkedInPostException handling ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenLinkedInPostExceptionThrown_RethrowsException()
    {
        // Arrange
        var postText = BuildLinkedInPostText();
        var linkedInException = new LinkedInPostException("API Error", 401, "Unauthorized");
        _linkedInManager
            .Setup(m => m.PostShareText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(linkedInException);

        var sut = BuildSut();

        // Act & Assert ΓÇö should rethrow LinkedInPostException
        await Assert.ThrowsAsync<LinkedInPostException>(() => sut.Run(postText));
    }

    // ΓöÇΓöÇ Generic exception handling ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

    [Fact]
    public async Task Run_WhenGenericExceptionThrown_RethrowsException()
    {
        // Arrange
        var postText = BuildLinkedInPostText();
        _linkedInManager
            .Setup(m => m.PostShareText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network timeout"));

        var sut = BuildSut();

        // Act & Assert ΓÇö should rethrow generic Exception
        await Assert.ThrowsAsync<Exception>(() => sut.Run(postText));
    }
}
