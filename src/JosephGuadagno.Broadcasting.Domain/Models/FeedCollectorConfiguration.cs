using System;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    public class FeedCollectorConfiguration: ConfigurationBase
    {
        public FeedCollectorConfiguration() : base(Constants.ConfigurationFunctionNames.CollectorsCheckForFeedUpdates)
        {
            
        }
        
        public DateTime LastCheckedFeed { get; set; }
    }
}