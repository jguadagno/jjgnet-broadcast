using System.Threading.Tasks;
using LinqToTwitter;
using Xunit;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class TwitterSendTweetTests
{

    private readonly TwitterContext _twitterContext;
    private readonly ITestOutputHelper _testOutputHelper;
        
    public TwitterSendTweetTests(TwitterContext twitterContext, ITestOutputHelper testOutputHelper)
    {
        _twitterContext = twitterContext;
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public async Task DoesSentTweet_ShouldSendTweet()
    {
        var tweet = await _twitterContext.TweetAsync("Test Tweet, Ignore");
        
        Assert.NotNull(tweet);
        
        // Clean up
        if (tweet is not null && tweet.ID is not null)
        {
            await _twitterContext.DeleteTweetAsync(tweet.ID);
        }
    }
}