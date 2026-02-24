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
    // GetAsync(DateTime sinceWhen)
    
    // Integration tests for GetSinceDate, GetSyndicationItems, and GetRandomSyndicationItem
    // have been moved to JosephGuadagno.Broadcasting.SyndicationFeedReader.IntegrationTests
    
}