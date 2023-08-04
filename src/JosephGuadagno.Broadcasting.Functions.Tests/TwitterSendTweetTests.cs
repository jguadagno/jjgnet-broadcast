using System.Threading.Tasks;
using LinqToTwitter;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class TwitterSendTweetTests
{

    private readonly TwitterContext _twitterContext;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger<TwitterSendTweetTests> _logger;

    public TwitterSendTweetTests(TwitterContext twitterContext, ITestOutputHelper testOutputHelper, ILogger<TwitterSendTweetTests> logger)
    {
        _twitterContext = twitterContext;
        _testOutputHelper = testOutputHelper;
        _logger = logger;
    }
    
    [Fact]
    public async Task DoesSendTweet_ShouldSendTweet()
    {
        var tweet = await _twitterContext.TweetAsync("Test Tweet, Ignore");
        
        Assert.NotNull(tweet);
        
        // Clean up
        if (tweet.ID is not null)
        {
            await _twitterContext.DeleteTweetAsync(tweet.ID);
        }
    }

    [Fact]
    public void ValidateLog()
    {
        _logger.LogTrace("Trace Message");
        _logger.LogDebug("Debug Message");
        _logger.LogInformation("Info Message");
        _logger.LogWarning("Warning Message");
        _logger.LogError("Error Message");
        _logger.LogCritical("Critical Message");
        _logger.LogMetric("LoggerMessage", 1);
    }
}