using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.IntegrationTests;

[Trait("Category", "Integration")]
public class SyndicationFeedReaderTests(
	ISyndicationFeedReader syndicationFeedReader,
	ITestOutputHelper testOutputHelper)
{
    private const string OwnerEntraOid = "integration-owner-entra-oid";
    private static readonly DateTimeOffset RandomPostCutoffDate = new(2019, 1, 1, 12, 0, 0, TimeSpan.FromHours(-7));
    private static readonly List<string> ExcludedCategories = ["books", "book reviews", "news", "arizona technology news", "technology news", "archive"];
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    // ### GetSinceDate Tests ###
    // GetSinceDate(DateTime sinceWhen)

    [Fact(Skip = "Manually run only")]
    public void GetSinceDate_WithValidParameters_ShouldReturnPosts()
    {
        // Arrange
        var sinceWhen = RandomPostCutoffDate;
        
        // Act
        var posts = syndicationFeedReader.GetSinceDate(OwnerEntraOid, sinceWhen);
        
        // Assert
        Assert.NotNull(posts);
        Assert.NotEmpty(posts);
    }
    
    [Fact(Skip = "Manually run only")]
    public void GetSinceDate_WithFutureSinceWhenDate_ShouldReturnNoPosts()
    {
        // Arrange
        var sinceWhen = DateTime.UtcNow.AddDays(1);
        
        // Act
        var posts = syndicationFeedReader.GetSinceDate(OwnerEntraOid, sinceWhen);
        
        // Assert
        Assert.NotNull(posts);
        Assert.Empty(posts);
    }
    
    [Fact(Skip = "Manually run only")]
    public void GetSinceDate_WithBadFeedUrl_ShouldThrowException()
    {
        // Arrange
        var syndicationFeedReader = new SyndicationFeedReader(
            new SyndicationFeedReaderSettings() { FeedUrl = "https://www.josephguadagno.net/fee.xml" },
            new LoggerFactory().CreateLogger<SyndicationFeedReader>());
        var sinceWhen = DateTime.UtcNow.AddDays(1);
        
        // Act
        Assert.Throws<HttpRequestException>(() =>
        {
            syndicationFeedReader.GetSinceDate(OwnerEntraOid, sinceWhen);
        });
        
        // Assert

    }
    
    // ### GetSyndicationItems Tests ###
    // GetSyndicationItems(DateTime sinceWhen, List<string> excludeCategories)
    // Test the GetSyndicationItems with no categories
    [Fact(Skip = "Manually run only")]
    public void GetSyndicationItem_WithAllExcludedCategories_ReturnsNonNullPost()
    {
        // Arrange
        
        // Act
        var randomPost = syndicationFeedReader.GetSyndicationItems(OwnerEntraOid, RandomPostCutoffDate, ExcludedCategories);

        // Assert
        Assert.NotNull(randomPost);
        Assert.NotEmpty(randomPost);
    }

    [Fact(Skip = "Manually run only")]
    public void GetSyndicationItem_WithNoExcludedCategories_ReturnsNonNullPost()
    {
        // Arrange
        
        // Act
        var randomPost = syndicationFeedReader.GetSyndicationItems(OwnerEntraOid, RandomPostCutoffDate, []);

        // Assert
        Assert.NotNull(randomPost);
        Assert.NotEmpty(randomPost);
    }
    
    [Fact(Skip = "Manually run only")]
    public void GetSyndicationItem_WithFuture_ReturnsNoPosts()
    {
        // Arrange
        
        // Act
        var randomPost = syndicationFeedReader.GetSyndicationItems(OwnerEntraOid, DateTime.UtcNow.AddDays(1), []);

        // Assert
        Assert.NotNull(randomPost);
        Assert.Empty(randomPost);
    }
    
    // ### GetRandomSyndicationItem Tests ###
    // GetRandomSyndicationItem(DateTime sinceWhen, List<string> excludeCategories)
    
    [Fact(Skip = "Manually run only")]
    public void GetRandomSyndicationItem_WithAllExcludedCategories_ReturnsNonNullItem()
    {
        // Arrange
        
        // Act
        var randomItem = syndicationFeedReader.GetRandomSyndicationItem(OwnerEntraOid, RandomPostCutoffDate, ExcludedCategories);

        // Assert
        Assert.NotNull(randomItem);
        Assert.NotNull(randomItem.Title);
    }
    
    [Fact(Skip = "Manually run only")]
    public void GetRandomSyndicationItem_WithNoExcludedCategories_ReturnsNonNullItem()
    {
        // Arrange
        
        // Act
        var randomItem = syndicationFeedReader.GetRandomSyndicationItem(OwnerEntraOid, RandomPostCutoffDate, []);

        // Assert
        Assert.NotNull(randomItem);
        Assert.NotNull(randomItem.Title);
    }
    
    [Fact(Skip = "Manually run only")]
    public void GetRandomSyndicationItem_WithFuture_ReturnsNull()
    {
        // Arrange
        
        // Act
        var randomItem = syndicationFeedReader.GetRandomSyndicationItem(OwnerEntraOid, DateTime.UtcNow.AddDays(1), []);

        // Assert
        Assert.Null(randomItem);
    }
    
    [Fact(Skip = "Manually run only")]
    public void GetRandomSyndicationItem_WithNullExcludedCategories_ReturnsNonNullItem()
    {
        // Arrange
        
        // Act
        var randomItem = syndicationFeedReader.GetRandomSyndicationItem(OwnerEntraOid, RandomPostCutoffDate, null);

        // Assert
        Assert.NotNull(randomItem);
        Assert.NotNull(randomItem.Title);
    }
    
}
