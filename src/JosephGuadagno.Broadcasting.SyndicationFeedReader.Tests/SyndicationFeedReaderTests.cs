using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Tests
{
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
        // Test the GetSyndicationItems
        // Test the GetSyndicationItems with no categories
        // Test the GetRandomSyndicationItem
        // Test the GetRandomSyndicationItem with no categories
        
        [Fact]
        public void Test1()
        {
            var randomPost = _syndicationFeedReader.GetRandomSyndicationItem(_randomPostSettings.CutoffDate,
                _randomPostSettings.ExcludedCategories);
            
            Assert.NotNull(randomPost);

        }
    }
}