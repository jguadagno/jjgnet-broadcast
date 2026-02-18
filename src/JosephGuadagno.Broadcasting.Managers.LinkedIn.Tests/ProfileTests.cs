using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests;

public class ProfileTests(
    ILinkedInManager linkedInManager,
    ILinkedInApplicationSettings linkedInApplicationSettings,
    ITestOutputHelper testOutputHelper)
{

    [Fact]
    public async Task GetMyLinkedInUserProfile_WithValidAccessToken_ReturnsAValidUserProfile()
    {
        // Arrange
        if (string.IsNullOrEmpty(linkedInApplicationSettings.AccessToken))
        {
            testOutputHelper.WriteLine("The LinkedIn Application Settings are null");
            Assert.Fail("Access Token can not be null");
            return;
        }
        
        // Act
        var myProfile = await linkedInManager.GetMyLinkedInUserProfile(linkedInApplicationSettings.AccessToken);
        
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
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => linkedInManager.GetMyLinkedInUserProfile(""));
        
        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'accessToken')", exception.Message);
    }
    
    [Fact]
    public async Task GetMyLinkedInUserProfile_WithInvalidAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        
        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => linkedInManager.GetMyLinkedInUserProfile("123456"));
        
        // Assert
        Assert.Equal("Invalid status code in the HttpResponseMessage: Unauthorized.", exception.Message);
    }
}