using System;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Twitter
{
    /// <summary>
    /// This is the configuration that is saved to and from Table Storage four each function
    /// </summary>
    public class Configuration: TableEntity
    {
        public Configuration()
        {
            PartitionKey = "Twitter";
            RowKey = "Configuration";
        }

        public DateTime LastCheckedFeed { get; set; }
    }
}