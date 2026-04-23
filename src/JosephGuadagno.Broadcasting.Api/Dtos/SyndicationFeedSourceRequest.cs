using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating a syndication feed source.
/// </summary>
public class SyndicationFeedSourceRequest
{
    /// <summary>
    /// The unique identifier for the feed item within the syndication feed (e.g., GUID or permalink).
    /// </summary>
    [Required]
    public string FeedIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The author or creator of the feed item.
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The title of the feed item.
    /// </summary>
    [Required]
    [StringLength(512)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The canonical URL of the feed item.
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The original publication date of the feed item.
    /// </summary>
    [Required]
    public DateTimeOffset PublicationDate { get; set; }

    /// <summary>
    /// An optional shortened URL for the feed item, used when posting to social media.
    /// </summary>
    [StringLength(255)]
    public string? ShortenedUrl { get; set; }

    /// <summary>
    /// Optional list of tags or categories associated with the feed item.
    /// </summary>
    public IList<string>? Tags { get; set; }
}
