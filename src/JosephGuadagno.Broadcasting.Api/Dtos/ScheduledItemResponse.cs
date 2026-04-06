using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a scheduled item.
/// </summary>
public class ScheduledItemResponse
{
    public int Id { get; set; }
    public ScheduledItemType ItemType { get; set; }
    public string ItemTableName { get; set; } = string.Empty;
    public int ItemPrimaryKey { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset? MessageSentOn { get; set; }
    public bool MessageSent { get; set; }
    public DateTimeOffset SendOnDateTime { get; set; }
    public string? Platform { get; set; }
    public string? MessageType { get; set; }
}
