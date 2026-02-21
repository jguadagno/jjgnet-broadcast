using System.Threading.Tasks;
using LinqToTwitter;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

[Trait("Category", "Integration")]
public class TwitterSendTweetTests(
    TwitterContext twitterContext)
{

    [Fact(Skip = "Integration test - requires external services")]
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