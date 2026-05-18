using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.IntegrationTests;

[Trait("Category", "Integration")]
public class FacebookPostPageStatusTest(
	IFacebookManager facebookManager,
	ITestOutputHelper testOutputHelper,
	ILogger<FacebookPostPageStatusTest> logger)
{
	private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;    
    private readonly ILogger<FacebookPostPageStatusTest> _logger = logger;


    [Fact(Skip = "Manually run only")]
    public async Task PostMessageAndLinkToPage_WithValidParameters_ShouldPostStatus()
    {
        // Arrange
        var message = "Test Message";
        var link = "https://josephguadagno.net";
        
        // Act
        var pageId = await facebookManager.PostMessageAndLinkToPage(message, link);

        // Assert
        Assert.False(string.IsNullOrEmpty(pageId));
    }

    [Fact(Skip = "Manually run only")]
    public async Task PostMessageLinkToPage_WithLongText_ShouldPostStatus()
    {
        // Arrange
        var message =
            "ICYMI: (9/6/2020): 'Dependency Injection with Azure Functions.'   #Azure #DependencyInjection #Functions";
        var link = "https://josephguadagno.net/2020/09/06/dependency-injection-with-azure-functions/";
        
        // Act
        var pageId = await facebookManager.PostMessageAndLinkToPage(message, link);

        // Assert
        Assert.False(string.IsNullOrEmpty(pageId));
        
    }

    [Fact(Skip = "Manually run only")]
    public async Task PostMessageAndLinkToPage_WithMessageEmpty_ShouldThrowException()
    {
        // Arrange
        var message = string.Empty;
        var link = "https://josephguadagno.net";
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            facebookManager.PostMessageAndLinkToPage(message, link));

        // Assert
        Assert.StartsWith("Value cannot be null.", exception.Message);
    }
    
    [Fact(Skip = "Manually run only")]
    public async Task PostMessageAndLinkToPage_WithLinkEmpty_ShouldThrowException()
    {
        // Arrange
        var message = "Test Message";
        var link = string.Empty;
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            facebookManager.PostMessageAndLinkToPage(message, link));

        // Assert
        Assert.StartsWith("Value cannot be null.", exception.Message);
    }
}