using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Tests;

public class BlueskyPostTests
{
    private readonly IBlueskyManager _blueskyManager;
    private readonly ITestOutputHelper _testOutputHelper;    
    private readonly ILogger<BlueskyPostTests> _logger;
    
    public BlueskyPostTests(IBlueskyManager blueskyManager, ITestOutputHelper testOutputHelper, ILogger<BlueskyPostTests> logger)
    {
        _blueskyManager = blueskyManager;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }
    
    [Fact]
    public async Task SendBlueskyPost_Success()
    {
        // Arrange
        var message = "Test message - Testing in Production";
        
        // Act
        var response = await _blueskyManager.PostText(message);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Cid);
        
        // Clean up
        await _blueskyManager.DeletePost(response.StrongReference);
    }
}