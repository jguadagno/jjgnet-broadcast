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
        
        public DateTime LastCheckedFeed { get; set; }
    }
}