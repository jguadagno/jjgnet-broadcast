using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests;

public class PostShareTests
{
    private readonly ILinkedInManager _linkedInManager;
    private readonly ILinkedInApplicationSettings _linkedInApplicationSettings;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger<PostShareTests> _logger;
    
    public PostShareTests(ILinkedInManager linkedInManager, ILinkedInApplicationSettings linkedInApplicationSettings, ITestOutputHelper testOutputHelper, ILogger<PostShareTests> logger)
    {
        _linkedInManager = linkedInManager;
        _linkedInApplicationSettings = linkedInApplicationSettings;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }

    // PostShareText Tests
    
    [Fact]
    public async Task PostShareText_WithValidParameter_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var id = await _linkedInManager.PostShareText(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Playing around with the LinkedIn API at {DateTime.Now}");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
        
    }
    
    [Fact]
    public async Task PostShareText_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareText(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,""));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'postText')", exception.Message);
    }
    
    [Fact]
    public async Task PostShareText_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareText("", _linkedInApplicationSettings.AuthorId, "Sample Post Text"));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }
    
    // PostShareTextAndLink Tests
    
    [Fact]
    public async Task PostShareTextAndLink_WithValidLink_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var id = await _linkedInManager.PostShareTextAndLink(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }
    
    [Fact]
    public async Task PostShareTextAndLink_WithValidLinkAndTitle_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var id = await _linkedInManager.PostShareTextAndLink(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net", "Joseph Guadagno Blog");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }
    
    [Fact]
    public async Task PostShareTextAndLink_WithValidLinkAndDescription_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var id = await _linkedInManager.PostShareTextAndLink(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net", "", "Description of link");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }
    
    [Fact]
    public async Task PostShareTextAndLink_WithValidLinkTitleAndDescription_ReturnId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var id = await _linkedInManager.PostShareTextAndLink(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", "https://josephguadagno.net", "Joseph Guadagno Blog", "Description of link");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact]
    public async Task PostShareTextAndLink_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareTextAndLink("", _linkedInApplicationSettings.AuthorId,"Sample Post Text", "https://josephguadagno.net"));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }

    [Fact]
    public async Task PostShareTextAndLink_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareTextAndLink(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,"", "https://josephguadagno.net"));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'postText')", exception.Message);
    }
    
    [Fact]
    public async Task PostShareTextAndLink_WithEmptyLink_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareTextAndLink(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,"Sample Post Text", ""));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'link')", exception.Message);
    }
    
    // PostShareTextAndImage Tests
    
    [Fact]
    public async Task PostShareTextAndImage_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareTextAndImage("", _linkedInApplicationSettings.AuthorId,"Sample Post Text", Array.Empty<byte>()));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }

    [Fact]
    public async Task PostShareTextAndImage_WithEmptyPostText_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareTextAndImage(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,"", Array.Empty<byte>()));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'postText')", exception.Message);
    }
    
    [Fact]
    public async Task PostShareTextAndImage_WithEmptyImage_ThrowsArgumentNullException()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.PostShareTextAndImage(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,"Sample Post", Array.Empty<byte>()));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'image')", exception.Message);
    }
    
    [Fact]
    public async Task PostShareTextAndImage_WithValidPostTextAndImage_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await _linkedInManager.PostShareTextAndImage(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes);
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }

    [Fact]
    public async Task PostShareTextAndImage_WithValidPostTextImageAndTitle_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await _linkedInManager.PostShareTextAndImage(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes, "Image Title: Coding with JoeG");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }
    
    [Fact]
    public async Task PostShareTextAndImage_WithValidPostTextImageAndDescription_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await _linkedInManager.PostShareTextAndImage(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes, "","Image Description: Coding with JoeG");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }
    
    [Fact]
    public async Task PostShareTextAndImage_WithValidPostTextImageTitleAndDescription_ReturnsId()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AccessToken' is null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AuthorId))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Setting of 'AuthorId' is null");
            Assert.Fail("AuthorId can not be null");
            return;
        }
        
        var fileBytes = await File.ReadAllBytesAsync("coding-with-joeg-reboot.png");

        // Act
        var id = await _linkedInManager.PostShareTextAndImage(_linkedInApplicationSettings.AccessToken, _linkedInApplicationSettings.AuthorId,$"Sample post from the LinkedIn Api. To be deleted. {DateTime.UtcNow}", fileBytes, "Image Title: Coding with JoeG", "Image Description: Coding with JoeG");
        
        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.StartsWith("urn:li:share:", id);
    }
    
}