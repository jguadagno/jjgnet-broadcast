namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a talk.
/// </summary>
public class TalkResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UrlForConferenceTalk { get; set; } = string.Empty;
    public string UrlForTalk { get; set; } = string.Empty;
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public string TalkLocation { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public int EngagementId { get; set; }
}
