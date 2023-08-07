using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class FacebookPostPageStatusTest
{
    private readonly IFacebookManager _facebookManager;
    private readonly ISettings _settings;
    private readonly ITestOutputHelper _testOutputHelper;    
    private readonly ILogger<FacebookPostPageStatusTest> _logger;
    

    public FacebookPostPageStatusTest(IFacebookManager facebookManager, ISettings settings, ITestOutputHelper testOutputHelper, ILogger<FacebookPostPageStatusTest> logger)
    {
        _facebookManager = facebookManager;
        _settings = settings;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }
    
    [Fact]
    public async Task PostMessageAndLinkToPage_WithValidParameters_ShouldPostStatus()
    {
        // Arrange
        var message = "Test Message";
        var link = "https://josephguadagno.net";
        
        // Act
        var pageId = await _facebookManager.PostMessageAndLinkToPage(_settings.FacebookPageId, message, link,
            _settings.FacebookPageAccessToken);

        // Assert
        Assert.False(string.IsNullOrEmpty(pageId));
    }

    [Fact]
    public async Task PostMessageLinkToPage_WithLongText_ShouldPostStatus()
    {
        // Arrange
        var message =
            "ICYMI: (9/6/2020): 'Dependency Injection with Azure Functions.'   #Azure #DependencyInjection #Functions";
        var link = "https://josephguadagno.net/2020/09/06/dependency-injection-with-azure-functions/";
        
        // Act
        var pageId = await _facebookManager.PostMessageAndLinkToPage(_settings.FacebookPageId, message, link,
            _settings.FacebookPageAccessToken);

        // Assert
        Assert.False(string.IsNullOrEmpty(pageId));
        
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_WithBadParameters_ShouldThrowException()
    {
        // Arrange
        var message = "Test Message";
        var link = "https://josephguadagno.net";
        
        // Act
        var exception = await Assert.ThrowsAsync<ApplicationException>(() =>
            _facebookManager.PostMessageAndLinkToPage(_settings.FacebookPageId, message, link, "bad-access-token"));

        // Assert
        Assert.StartsWith("Failed to post status. ", exception.Message);
    }
}