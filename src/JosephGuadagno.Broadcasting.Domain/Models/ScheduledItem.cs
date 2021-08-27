using System;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    /// <summary>
    /// A item that has been scheduled to be sent our
    /// </summary>
    public class ScheduledItem: TableEntity, IScheduledItem
    {
        public ScheduledItem()
        {
        }

        public ScheduledItem(string sourceTableName) : base(sourceTableName, Guid.NewGuid().ToString())
        {
            
        }

        public ScheduledItem(string sourceTableName, string id)
        {
            if (string.IsNullOrEmpty(sourceTableName))
            {
                throw new ArgumentNullException(nameof(sourceTableName), "The source table name is required");
            }
            PartitionKey = sourceTableName;
            RowKey = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString(): id;
        }

        /// <summary>
        /// The identifier of the row
        /// </summary>
        public string Id => RowKey;
        
        /// <summary>
        /// The table name where the item is stored
        /// </summary>
        /// <remarks>
        /// This could be SourceData, Engagements, or more
        /// </remarks>
        public string SourceTableName { get; set; }
        
        /// <summary>
        /// The partition key in the <see cref="SourceTableName"/>
        /// </summary>
        public string ItemPartitionKey { get; set; }
        
        /// <summary>
        /// The RowKey in the <see cref="SourceTableName"/>
        /// </summary>
        public string ItemRoyKey { get; set; }
        
        /// <summary>
        /// The date and time this item is scheduled to go out
        /// </summary>
        public DateTime ScheduleDateTime { get; set; }
    }
}