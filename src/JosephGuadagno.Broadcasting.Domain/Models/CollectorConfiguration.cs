using System;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    public class CollectorConfiguration: ConfigurationBase
    {
        public CollectorConfiguration() {}
        
        public CollectorConfiguration(string collectorName)
        {
            if (string.IsNullOrEmpty(collectorName))
            {
                throw new ArgumentNullException(nameof(collectorName), "The collector name must be specified.");
            }

            RowKey = collectorName;
        }
        
        /// <summary>
        /// The date and time the feed was last checked
        /// </summary>
        /// <remarks>The date and time should be in UTC</remarks>
        public DateTime LastCheckedFeed { get; set; }
    }
}