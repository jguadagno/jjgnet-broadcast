using System.Runtime.CompilerServices;

using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests;

[Trait("Category", "Integration")]
public class PostShareTests(
    ILinkedInManager linkedInManager,
    ILinkedInApplicationSettings linkedInApplicationSettings,
    ITestOutputHelper testOutputHelper)
{

    // PostShareText Tests
    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareText_WithValidParameter_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var id = await linkedInManager.PostShareText(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Playing around with the LinkedIn API at {DateTime.Now}");

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);

    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareText_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareText(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,""));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'postText')", exception.Message);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareText_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareText("", linkedInApplicationSettings.AuthorId, "Sample Post Text"));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }

    // PostShareTextAndLink Tests

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndLink_WithValidLink_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var id = await linkedInManager.PostShareTextAndLink(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net");

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndLink_WithValidLinkAndTitle_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var id = await linkedInManager.PostShareTextAndLink(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net", "Joseph Guadagno Blog");

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndLink_WithValidLinkAndDescription_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var id = await linkedInManager.PostShareTextAndLink(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net", "", "Description of link");

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndLink_WithValidLinkTitleAndDescription_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var id = await linkedInManager.PostShareTextAndLink(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net", "Joseph Guadagno Blog", "Description of link");

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndLink_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareTextAndLink("", linkedInApplicationSettings.AuthorId,"Sample Post Text", "https://josephguadagno.net"));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndLink_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareTextAndLink(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,"", "https://josephguadagno.net"));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'postText')", exception.Message);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndLink_WithEmptyLink_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareTextAndLink(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,"Sample Post Text", ""));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'link')", exception.Message);
    }

    // PostShareTextAndImage Tests

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndImage_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareTextAndImage("", linkedInApplicationSettings.AuthorId,"Sample Post Text", Array.Empty<byte>()));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndImage_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareTextAndImage(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,"", Array.Empty<byte>()));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'postText')", exception.Message);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndImage_WithEmptyImage_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.PostShareTextAndImage(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,"Sample Post", Array.Empty<byte>()));

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'image')", exception.Message);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndImage_WithValidPostTextAndImage_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await linkedInManager.PostShareTextAndImage(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes);

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndImage_WithValidPostTextImageAndTitle_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await linkedInManager.PostShareTextAndImage(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes, "Image Title: Coding with JoeG");

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndImage_WithValidPostTextImageAndDescription_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await linkedInManager.PostShareTextAndImage(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes, "","Image Description: Coding with JoeG");

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostShareTextAndImage_WithValidPostTextImageTitleAndDescription_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AuthorId))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }

        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await linkedInManager.PostShareTextAndImage(linkedInApplicationSettings.AccessToken, linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes, "Image Title: Coding with JoeG", "Image Description: Coding with JoeG");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }
    
}