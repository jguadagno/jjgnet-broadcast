using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a talk.
/// </summary>
public class TalkRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    public string UrlForConferenceTalk { get; set; } = string.Empty;

    [Required]
    [Url]
    public string UrlForTalk { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset StartDateTime { get; set; }

    [Required]
    public DateTimeOffset EndDateTime { get; set; }

    public string TalkLocation { get; set; } = string.Empty;

    public string Comments { get; set; } = string.Empty;
}
