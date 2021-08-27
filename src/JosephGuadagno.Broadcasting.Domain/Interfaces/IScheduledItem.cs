using System;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces
{
    /// <summary>
    /// A item that has been scheduled to be sent our
    /// </summary>
    public interface IScheduledItem
    {
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