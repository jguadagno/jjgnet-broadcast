namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a talk (session) within a speaking engagement, returned by the talk endpoints
/// and embedded in <see cref="EngagementResponse.Talks"/>.
/// </summary>
public class TalkResponse
{
    /// <summary>
    /// The unique identifier of the talk record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The title of the talk or session as it appears in the conference program.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the talk's listing on the conference or event website.
    /// </summary>
    public string UrlForConferenceTalk { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the talk's primary resource, such as the slides deck, recording, or abstract page.
    /// </summary>
    public string UrlForTalk { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the talk begins, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// </summary>
    public DateTimeOffset StartDateTime { get; set; }

    /// <summary>
    /// The date and time when the talk ends, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// </summary>
    public DateTimeOffset EndDateTime { get; set; }

    /// <summary>
    /// The physical location of the talk, such as the room name or hall number within the venue.
    /// </summary>
    public string TalkLocation { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes or context about the talk (e.g., abstract, co-presenters, prerequisites).
    /// </summary>
    public string Comments { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the parent engagement (conference or event) this talk belongs to.
    /// </summary>
    public int EngagementId { get; set; }
}
