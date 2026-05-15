using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a talk (session) within a speaking engagement.
/// Used by the <c>POST /engagements/{id}/talks</c> and <c>PUT /engagements/{id}/talks/{talkId}</c> endpoints.
/// </summary>
public class TalkRequest
{
    /// <summary>
    /// The title of the talk or session as it appears in the conference program.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the talk's listing on the conference or event website. Must be a valid absolute URL.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [Url]
    public string UrlForConferenceTalk { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the talk's primary resource, such as the slides deck, recording, or abstract page.
    /// Must be a valid absolute URL.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [Url]
    public string UrlForTalk { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the talk begins, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public DateTimeOffset StartDateTime { get; set; }

    /// <summary>
    /// The date and time when the talk ends, expressed as a <see cref="DateTimeOffset"/>
    /// that includes the UTC offset for the engagement's local timezone.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public DateTimeOffset EndDateTime { get; set; }

    /// <summary>
    /// The physical location of the talk, such as the room name or hall number within the venue.
    /// </summary>
    public string TalkLocation { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes or context about the talk (e.g., abstract, co-presenters, prerequisites).
    /// </summary>
    public string Comments { get; set; } = string.Empty;
}
