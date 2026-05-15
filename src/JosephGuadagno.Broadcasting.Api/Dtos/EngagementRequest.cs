using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a speaking engagement. Used by the
/// <c>POST /engagements</c> and <c>PUT /engagements/{id}</c> endpoints.
/// </summary>
public class EngagementRequest
{
    /// <summary>
    /// The name or title of the speaking engagement (e.g., the conference or event name).
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL for the engagement's official website or event page. Must be a valid absolute URL.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the engagement begins, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public DateTimeOffset StartDateTime { get; set; }

    /// <summary>
    /// The date and time when the engagement ends, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public DateTimeOffset EndDateTime { get; set; }

    /// <summary>
    /// The IANA or Windows timezone identifier for the engagement's local time
    /// (e.g., <c>"America/Phoenix"</c> or <c>"US Mountain Standard Time"</c>).
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes or additional details about the engagement (e.g., venue address, logistics).
    /// </summary>
    public string? Comments { get; set; }
}
