using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Tests;

public class SyndicationFeedReaderTests(
	ISyndicationFeedReader syndicationFeedReader,
	ITestOutputHelper testOutputHelper)
{
    private readonly ISyndicationFeedReader _syndicationFeedReader = syndicationFeedReader;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

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
            var syndicationFeedReader = new SyndicationFeedReader(null!, logger);
        });
    }
    
    [Fact]
    public void Constructor_WithNullFeedUrl_ShouldNotThrow()
    {
        // Arrange — FeedUrl is now optional in the constructor; per-user URL is passed at call time
        var logger = new LoggerFactory().CreateLogger<SyndicationFeedReader>();
        var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings();

        // Act & Assert — no exception expected
        var syndicationFeedReader = new SyndicationFeedReader(syndicationFeedReaderSettings, logger);
        Assert.NotNull(syndicationFeedReader);
    }

    [Fact]
    public async Task GetAsync_WithNullPerUserFeedUrl_ShouldThrowArgumentException()
    {
        // Arrange — per-user overload must validate feedUrl
        var logger = new LoggerFactory().CreateLogger<SyndicationFeedReader>();
        var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings();
        var reader = new SyndicationFeedReader(syndicationFeedReaderSettings, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            reader.GetAsync(null!, "oid-123", DateTimeOffset.UtcNow));
    }
        
    // ### GetSinceDate Tests ###
    // GetAsync(DateTime sinceWhen)
    
    // Integration tests for GetSinceDate, GetSyndicationItems, and GetRandomSyndicationItem
    // have been moved to JosephGuadagno.Broadcasting.SyndicationFeedReader.IntegrationTests
    
}