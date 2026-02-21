using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Tests;

[Trait("Category", "Integration")]
public class FacebookPostPageStatusTest
{
    private readonly IFacebookManager _facebookManager;
    private readonly ITestOutputHelper _testOutputHelper;    
    private readonly ILogger<FacebookPostPageStatusTest> _logger;
    

    public FacebookPostPageStatusTest(IFacebookManager facebookManager, ITestOutputHelper testOutputHelper, ILogger<FacebookPostPageStatusTest> logger)
    {
        _facebookManager = facebookManager;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }
    
    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostMessageAndLinkToPage_WithValidParameters_ShouldPostStatus()
    {
        // Arrange
        var message = "Test Message";
        var link = "https://josephguadagno.net";
        
        // Act
        var pageId = await _facebookManager.PostMessageAndLinkToPage(message, link);

        // Assert
        Assert.False(string.IsNullOrEmpty(pageId));
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostMessageLinkToPage_WithLongText_ShouldPostStatus()
    {
        // Arrange
        var message =
            "ICYMI: (9/6/2020): 'Dependency Injection with Azure Functions.'   #Azure #DependencyInjection #Functions";
        var link = "https://josephguadagno.net/2020/09/06/dependency-injection-with-azure-functions/";
        
        // Act
        var pageId = await _facebookManager.PostMessageAndLinkToPage(message, link);

        // Assert
        Assert.False(string.IsNullOrEmpty(pageId));
        
    }

    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostMessageAndLinkToPage_WithMessageEmpty_ShouldThrowException()
    {
        // Arrange
        var message = string.Empty;
        var link = "https://josephguadagno.net";
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _facebookManager.PostMessageAndLinkToPage(message, link));

        // Assert
        Assert.StartsWith("Value cannot be null.", exception.Message);
    }
    
    [Fact(Skip = "Integration test - requires external services")]
    public async Task PostMessageAndLinkToPage_WithLinkEmpty_ShouldThrowException()
    {
        // Arrange
        var message = "Test Message";
        var link = string.Empty;
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _facebookManager.PostMessageAndLinkToPage(message, link));

        // Assert
        Assert.StartsWith("Value cannot be null.", exception.Message);
    }
}