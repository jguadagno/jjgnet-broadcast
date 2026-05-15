namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a speaking engagement, returned by the engagement endpoints.
/// Contains full engagement details including associated talks.
/// </summary>
public class EngagementResponse
{
    /// <summary>
    /// The unique identifier of the engagement record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name or title of the speaking engagement (e.g., the conference or event name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL for the engagement's official website or event page.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the engagement begins, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// </summary>
    public DateTimeOffset StartDateTime { get; set; }

    /// <summary>
    /// The date and time when the engagement ends, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// </summary>
    public DateTimeOffset EndDateTime { get; set; }

    /// <summary>
    /// The IANA or Windows timezone identifier for the engagement's local time
    /// (e.g., <c>"America/Phoenix"</c> or <c>"US Mountain Standard Time"</c>).
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes or additional details about the engagement.
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// The list of talks or sessions associated with this engagement. May be null if talks were not requested.
    /// </summary>
    public List<TalkResponse>? Talks { get; set; }

    /// <summary>
    /// The date and time when this record was first created, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// The date and time when this record was most recently updated, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
