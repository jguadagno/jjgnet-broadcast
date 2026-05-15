using System.ComponentModel.DataAnnotations;
using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a scheduled broadcast item. Used by the
/// <c>POST /schedules</c> and <c>PUT /schedules/{id}</c> endpoints.
/// </summary>
public class ScheduledItemRequest
{
    /// <summary>
    /// The type of source item to be broadcast (e.g., talk, YouTube video, blog post).
    /// Determines which content table is used for lookup.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public ScheduledItemType ItemType { get; set; }

    /// <summary>
    /// The database primary key of the source item to broadcast. Combined with <see cref="ItemType"/>
    /// to locate the specific content record.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public int ItemPrimaryKey { get; set; }

    /// <summary>
    /// The message body to send when this item is broadcast to social media.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// An optional URL to an image to attach when broadcasting this item to supported platforms.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The date and time when the message should be sent, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public DateTimeOffset SendOnDateTime { get; set; }

    /// <summary>
    /// The target social media platform name (e.g., <c>"Twitter"</c>). When null, the item
    /// is broadcast to all platforms configured for the user.
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// The message template type to use when composing the broadcast (e.g., <c>"NewPost"</c>).
    /// Optional; when null, the default template for the platform is used.
    /// </summary>
    public string? MessageType { get; set; }
}
