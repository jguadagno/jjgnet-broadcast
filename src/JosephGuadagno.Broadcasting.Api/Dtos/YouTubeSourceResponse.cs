namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a YouTube source.
/// </summary>
public class YouTubeSourceResponse
{
    /// <summary>
    /// The unique identifier for the YouTube source record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The YouTube video ID (e.g., the part after <c>?v=</c> in the watch URL).
    /// </summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>
    /// The author or creator of the YouTube video.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The title of the YouTube video.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The canonical URL of the YouTube video.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The original publication date of the YouTube video.
    /// </summary>
    public DateTimeOffset PublicationDate { get; set; }

    /// <summary>
    /// An optional shortened URL for the video, used when posting to social media.
    /// </summary>
    public string? ShortenedUrl { get; set; }

    /// <summary>
    /// The list of tags or categories associated with the YouTube video.
    /// </summary>
    public IList<string> Tags { get; set; } = [];

    /// <summary>
    /// The date and time when this record was added to the system.
    /// </summary>
    public DateTimeOffset AddedOn { get; set; }

    /// <summary>
    /// The date and time when this record was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who created this record.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
}
