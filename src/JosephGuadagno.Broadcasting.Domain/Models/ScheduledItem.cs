using System.ComponentModel.DataAnnotations;
using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// A item that has been scheduled to be sent out
/// </summary>
public class ScheduledItem
{

    /// <summary>
    /// The identifier of the row
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The type of item that is scheduled to be sent out.
    /// </summary>
    [Required]
    public ScheduledItemType ItemType { get; set; }

    /// <summary>
    /// The table name where the item is stored.
    /// Derived from <see cref="ItemType"/> for backward compatibility.
    /// </summary>
    public string ItemTableName => ItemType.ToString();
        
    /// <summary>
    /// The primary key for this record.
    /// </summary>
    [Required]
    public int ItemPrimaryKey { get; set; }
        
    /// <summary>
    /// The message that will be sent out
    /// </summary>
    [Required]
    public string Message { get; set; }

    /// <summary>
    /// An optional URL of an image to attach or embed in the broadcast post.
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// When the message was sent by the scheduler
    /// </summary>
    public DateTimeOffset? MessageSentOn { get; set; }
    
    /// <summary>
    /// Indicates if the message was sent
    /// </summary>
    public bool MessageSent { get; set; }
    
    /// <summary>
    /// The date and time this item is scheduled to go out
    /// </summary>
    [Required]
    public DateTimeOffset SendOnDateTime { get; set; }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { "Id", Id.ToString() },
            { "ItemTableName", ItemTableName },
            { "ItemPrimaryKey", ItemPrimaryKey.ToString() },
            { "ScheduledDate", SendOnDateTime.ToString("f") }
        };
    }
}