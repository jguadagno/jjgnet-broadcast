using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating a YouTube source.
/// </summary>
public class YouTubeSourceRequest
{
    /// <summary>
    /// The YouTube video ID (e.g., the part after <c>?v=</c> in the watch URL).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string VideoId { get; set; } = string.Empty;

    /// <summary>
    /// The author or creator of the YouTube video.
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The title of the YouTube video.
    /// </summary>
    [Required]
    [StringLength(512)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The canonical URL of the YouTube video.
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The original publication date of the YouTube video.
    /// </summary>
    [Required]
    public DateTimeOffset PublicationDate { get; set; }

    /// <summary>
    /// An optional shortened URL for the video, used when posting to social media.
    /// </summary>
    [StringLength(255)]
    public string? ShortenedUrl { get; set; }

    /// <summary>
    /// Optional list of tags or categories associated with the YouTube video.
    /// </summary>
    public IList<string>? Tags { get; set; }
}
