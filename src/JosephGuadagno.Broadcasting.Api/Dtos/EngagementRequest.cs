using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating an engagement.
/// </summary>
public class EngagementRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset StartDateTime { get; set; }

    [Required]
    public DateTimeOffset EndDateTime { get; set; }

    [Required]
    public string TimeZoneId { get; set; } = string.Empty;

    public string? Comments { get; set; }
}
