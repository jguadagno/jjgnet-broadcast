using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// A item that has been scheduled to be sent out
/// </summary>
public class ScheduledItemViewModel
{

    /// <summary>
    /// The identifier of the row
    /// </summary>
    public int Id { get; set; }
        
    /// <summary>
    /// The table name where the item is stored
    /// </summary>
    /// <remarks>
    /// This could be SourceData, Engagements, or more
    /// </remarks>
    public string ItemTableName { get; set; }
        
    /// <summary>
    /// The primary key for this record.
    /// </summary>
    /// <remarks>
    /// For <see cref="SourceData"/> it is the PartitionKey.
    /// For <see cref="EngagementViewModel"/> it is the Id field/>
    /// </remarks>
    public string ItemPrimaryKey { get; set; }
        
    /// <summary>
    /// The secondary key for the record
    /// </summary>
    /// <remarks>
    /// For <see cref="SourceData"/> it is the RowKey.
    /// For <see cref="EngagementViewModel"/> it is not applicable.
    /// </remarks>
    public string ItemSecondaryKey { get; set; }
        
    /// <summary>
    /// The message that will be sent out
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// When the message was sent by the scheduler
    /// </summary>
    public DateTimeOffset MessageSentOn { get; set; }
    
    /// <summary>
    /// Indicates if the message was sent
    /// </summary>
    public bool MessageSent { get; set; }
    
    /// <summary>
    /// The date and time this item is scheduled to go out
    /// </summary>
    public DateTimeOffset ScheduleDateTime { get; set; }

    /// <summary>
    /// Returns a Dictionary&lt;string, string&gt; representation of the properties
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { "Id", Id.ToString() },
            { "ItemTableName", ItemTableName },
            { "ItemPrimaryKey", ItemPrimaryKey },
            { "ItemSecondaryKey", ItemSecondaryKey },
            { "ScheduledDate", ScheduleDateTime.ToString() }
        };
    }
}