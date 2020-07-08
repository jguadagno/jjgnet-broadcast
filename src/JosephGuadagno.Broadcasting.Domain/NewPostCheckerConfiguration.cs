using System;

namespace JosephGuadagno.Broadcasting.Domain
{
    public class NewPostCheckerConfiguration: ConfigurationBase
    {
        public NewPostCheckerConfiguration() : base(Constants.ConfigurationFunctionNames.NewPostChecker)
        {
            
        }
        
        public DateTime LastCheckedFeed { get; set; }
    }
}