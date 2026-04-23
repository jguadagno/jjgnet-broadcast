namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a YouTube source.
/// </summary>
public class YouTubeSourceResponse
{
    public int Id { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset PublicationDate { get; set; }
    public string? ShortenedUrl { get; set; }
    public IList<string> Tags { get; set; } = [];
    public DateTimeOffset AddedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
}
