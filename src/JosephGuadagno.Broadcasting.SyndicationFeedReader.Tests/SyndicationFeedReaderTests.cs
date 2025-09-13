using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Tests;

public class SyndicationFeedReaderTests
{
    private readonly ISyndicationFeedReader _syndicationFeedReader;
    private readonly IRandomPostSettings _randomPostSettings;
    private readonly ITestOutputHelper _testOutputHelper;
        
    public SyndicationFeedReaderTests(ISyndicationFeedReader syndicationFeedReader, IRandomPostSettings randomPostSettings, ITestOutputHelper testOutputHelper)
    {
        _syndicationFeedReader = syndicationFeedReader;
        _randomPostSettings = randomPostSettings;
        _testOutputHelper = testOutputHelper;
    }
    
    // ### Constructor Tests ###
    // Constructor(ISyndicationFeedReaderSettings syndicationFeedReaderSettings)
    
    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrowException()
    {
        // Arrange
        var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings
        {
            FeedUrl = "https://josephguadagno.net/feed.xml"
        };
        var logger = new LoggerFactory().CreateLogger<SyndicationFeedReader>();
        
        // Act
        var syndicationFeedReader = new SyndicationFeedReader(syndicationFeedReaderSettings, logger);
        
        // Assert
        Assert.True(true);
    }
    
    [Fact]
    public void Constructor_WithNullFeedSettings_ShouldThrowException()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<SyndicationFeedReader>();
        
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            var syndicationFeedReader = new SyndicationFeedReader(null, logger);
        });
    }
    
    [Fact]
    public void Constructor_WithFeedSettingsUrlNull_ShouldThrowException()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<SyndicationFeedReader>();
        
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings();
            var syndicationFeedReader = new SyndicationFeedReader(syndicationFeedReaderSettings, logger);
        });
    }
    
    [Fact]
    public void Constructor_WithFeedSettingsUrlEmpty_ShouldThrowException()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<SyndicationFeedReader>();
        
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings{FeedUrl = string.Empty};
            var syndicationFeedReader = new SyndicationFeedReader(syndicationFeedReaderSettings, logger);
        });
    }
        
    // ### GetSinceDate Tests ###
    // GetSinceDate(DateTime sinceWhen)

    [Fact]
    public void GetSinceDate_WithValidParameters_ShouldReturnPosts()
    {
        // Arrange
        var sinceWhen = _randomPostSettings.CutoffDate;
        
        // Act
        var posts = _syndicationFeedReader.GetSinceDate(sinceWhen);
        
        // Assert
        Assert.NotNull(posts);
        Assert.NotEmpty(posts);
    }
    
    [Fact]
    public void GetSinceDate_WithFutureSinceWhenDate_ShouldReturnNoPosts()
    {
        // Arrange
        var sinceWhen = DateTime.UtcNow.AddDays(1);
        
        // Act
        var posts = _syndicationFeedReader.GetSinceDate(sinceWhen);
        
        // Assert
        Assert.NotNull(posts);
        Assert.Empty(posts);
    }
    
    [Fact]
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
            syndicationFeedReader.GetSinceDate(sinceWhen);
        });
        
        // Assert

    }
    
    // ### GetAsync Tests ###
    // GetAsync(DateTime sinceWhen)
    
    // Not testing this one, since it's just a wrapper for GetSinceDate
    
    // ### GetSyndicationItems Tests ###
    // GetSyndicationItems(DateTime sinceWhen, List<string> excludeCategories)
    // Test the GetSyndicationItems with no categories
    [Fact] 
    public void GetSyndicationItem_WithAllExcludedCategories_ReturnsNonNullPost()
    {
        // Arrange
        
        // Act
        var randomPost = _syndicationFeedReader.GetSyndicationItems(_randomPostSettings.CutoffDate, _randomPostSettings.ExcludedCategories);

        // Assert
        Assert.NotNull(randomPost);
        Assert.NotEmpty(randomPost);
    }

    [Fact]
    public void GetSyndicationItem_WithNoExcludedCategories_ReturnsNonNullPost()
    {
        // Arrange
        
        // Act
        var randomPost = _syndicationFeedReader.GetSyndicationItems(_randomPostSettings.CutoffDate, []);

        // Assert
        Assert.NotNull(randomPost);
        Assert.NotEmpty(randomPost);
    }
    
    [Fact]
    public void GetSyndicationItem_WithFuture_ReturnsNoPosts()
    {
        // Arrange
        
        // Act
        var randomPost = _syndicationFeedReader.GetSyndicationItems(DateTime.UtcNow.AddDays(1), []);

        // Assert
        Assert.NotNull(randomPost);
        Assert.Empty(randomPost);
    }
    
}