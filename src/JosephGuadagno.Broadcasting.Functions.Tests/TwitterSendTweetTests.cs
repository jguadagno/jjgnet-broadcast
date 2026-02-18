using System.Threading.Tasks;
using LinqToTwitter;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class TwitterSendTweetTests(
    TwitterContext twitterContext)
{

    [Fact]
    public async Task DoesSendTweet_ShouldSendTweet()
    {
        var tweet = await twitterContext.TweetAsync("Test Tweet, Ignore", cancelToken: TestContext.Current.CancellationToken);
        
        Assert.NotNull(tweet);
        
        // Clean up
        if (tweet.ID is not null)
        {
            await twitterContext.DeleteTweetAsync(tweet.ID, cancelToken: TestContext.Current.CancellationToken);
        }
    }

}