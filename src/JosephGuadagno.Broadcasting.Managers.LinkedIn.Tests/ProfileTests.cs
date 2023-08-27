using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests;

public class ProfileTests
{
    
    private readonly ILinkedInManager _linkedInManager;
    private readonly ILinkedInApplicationSettings _linkedInApplicationSettings;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger<ProfileTests> _logger;
    
    public ProfileTests(ILinkedInManager linkedInManager, ILinkedInApplicationSettings linkedInApplicationSettings, ITestOutputHelper testOutputHelper, ILogger<ProfileTests> logger)
    {
        _linkedInManager = linkedInManager;
        _linkedInApplicationSettings = linkedInApplicationSettings;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }
    
    [Fact]
    public async Task GetMyLinkedInUserProfile_WithValidAccessToken_ReturnsAValidUserProfile()
    {
        // Arrange
        if (string.IsNullOrEmpty(_linkedInApplicationSettings.AccessToken))
        {
            _testOutputHelper.WriteLine("The LinkedIn Application Settings are null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        
        // Act
        var myProfile = await _linkedInManager.GetMyLinkedInUserProfile(_linkedInApplicationSettings.AccessToken);
        
        // Assert
        Assert.NotNull(myProfile);
        Assert.Equal("Joseph", myProfile.FirstName);
        Assert.Equal("Guadagno", myProfile.LastName);
       
    }
    
    [Fact]
    public async Task GetMyLinkedInUserProfile_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _linkedInManager.GetMyLinkedInUserProfile(""));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }
    
    [Fact]
    public async Task GetMyLinkedInUserProfile_WithInvalidAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        
        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _linkedInManager.GetMyLinkedInUserProfile("123456"));
        
        // Assert
        Assert.Equal("Invalid status code in the HttpResponseMessage: Unauthorized.", exception.Message);
    }
}