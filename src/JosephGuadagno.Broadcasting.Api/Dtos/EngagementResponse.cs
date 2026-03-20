namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for an engagement.
/// </summary>
public class EngagementResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string? BlueSkyHandle { get; set; }
    public string? ConferenceHashtag { get; set; }
    public string? ConferenceTwitterHandle { get; set; }
    public List<TalkResponse>? Talks { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}
