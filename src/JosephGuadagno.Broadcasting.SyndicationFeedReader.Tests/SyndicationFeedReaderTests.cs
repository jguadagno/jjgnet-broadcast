using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

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
        
    // TODO: Test Required
    // Test the GetSyndicationItems with no categories
        
    [Fact]
    public void GetSyndicationItem_ReturnsNonNullPost()
    {
        var randomPost = _syndicationFeedReader.GetSyndicationItems(_randomPostSettings.CutoffDate, _randomPostSettings.ExcludedCategories);
            
        Assert.NotNull(randomPost);
        Assert.NotEmpty(randomPost);
    }
}