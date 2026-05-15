using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a scheduled broadcast item, returned by the schedule endpoints.
/// Includes delivery status and human-readable source item details.
/// </summary>
public class ScheduledItemResponse
{
    /// <summary>
    /// The unique identifier of the scheduled item record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The type of source item to be broadcast (e.g., talk, YouTube video, blog post).
    /// </summary>
    public ScheduledItemType ItemType { get; set; }

    /// <summary>
    /// The database table name corresponding to <see cref="ItemType"/>, used to identify the source content table.
    /// </summary>
    public string ItemTableName { get; set; } = string.Empty;

    /// <summary>
    /// The database primary key of the source item to broadcast.
    /// </summary>
    public int ItemPrimaryKey { get; set; }

    /// <summary>
    /// The message body that will be (or was) sent when this item was broadcast.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The URL of an image attached to the broadcast, or null if no image is included.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The date and time when the message was sent, expressed as a <see cref="DateTimeOffset"/>.
    /// Null if the message has not been sent yet.
    /// </summary>
    public DateTimeOffset? MessageSentOn { get; set; }

    /// <summary>
    /// Indicates whether this scheduled message has already been sent.
    /// </summary>
    public bool MessageSent { get; set; }

    /// <summary>
    /// The date and time when the message is scheduled to be sent, expressed as a <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset SendOnDateTime { get; set; }

    /// <summary>
    /// The target social media platform name (e.g., <c>"Twitter"</c>). Null indicates broadcast
    /// to all configured platforms.
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// The message template type used when composing the broadcast (e.g., <c>"NewPost"</c>).
    /// Null when the default template was applied.
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// A human-readable display name for the source item (e.g., the talk title or video title),
    /// populated for display purposes only.
    /// </summary>
    public string? SourceItemDisplayName { get; set; }
}
