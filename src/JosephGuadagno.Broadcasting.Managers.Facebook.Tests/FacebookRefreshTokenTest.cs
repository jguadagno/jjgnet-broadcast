using System;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Tests;

public class FacebookRefreshTokenTest
{
    private readonly IFacebookManager _facebookManager;
    private readonly IFacebookApplicationSettings _facebookApplicationSettings;
    private readonly ITestOutputHelper _testOutputHelper;    
    private readonly ILogger<FacebookRefreshTokenTest> _logger;
    

    public FacebookRefreshTokenTest(IFacebookManager facebookManager, IFacebookApplicationSettings facebookApplicationSettings, ITestOutputHelper testOutputHelper, ILogger<FacebookRefreshTokenTest> logger)
    {
        _facebookManager = facebookManager;
        _facebookApplicationSettings = facebookApplicationSettings;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }
    
    [Fact]
    public async Task RefreshToken_WithValidParameters_ShouldReturnTokenInfo()
    {
        // Arrange
        var tokenToRefresh = _facebookApplicationSettings.LongLivedAccessToken;
        if (string.IsNullOrEmpty(tokenToRefresh))
        {
            _testOutputHelper.WriteLine("The LongLivedAccessToken is not set. Skipping test.");
            Assert.Fail();
            return;
        }
        
        // Act
        var tokenInfo = await _facebookManager.RefreshToken(tokenToRefresh);

        // Assert
        Assert.NotNull(tokenInfo);
        Assert.NotNull(tokenInfo.AccessToken);
        Assert.NotEqual(tokenInfo.AccessToken, tokenToRefresh);
        Assert.NotNull(tokenInfo.TokenType);
        Assert.True(tokenInfo.ExpiresOn > DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshToken_WithTokenEmpty_ShouldThrowException()
    {
        // Arrange
        var tokenToRefresh = string.Empty;
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _facebookManager.RefreshToken(tokenToRefresh));

        // Assert
        Assert.StartsWith("Value cannot be null.", exception.Message);
    }
    
}