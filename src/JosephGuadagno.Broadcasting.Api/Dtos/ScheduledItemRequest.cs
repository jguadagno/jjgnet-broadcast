using System.ComponentModel.DataAnnotations;
using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a scheduled item.
/// </summary>
public class ScheduledItemRequest
{
    [Required]
    public ScheduledItemType ItemType { get; set; }

    [Required]
    public int ItemPrimaryKey { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    [Required]
    public DateTimeOffset SendOnDateTime { get; set; }

    public string? Platform { get; set; }

    public string? MessageType { get; set; }
}
