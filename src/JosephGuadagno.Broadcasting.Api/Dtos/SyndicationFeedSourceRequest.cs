using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating a syndication feed source.
/// </summary>
public class SyndicationFeedSourceRequest
{
    [Required]
    public string FeedIdentifier { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset PublicationDate { get; set; }

    [StringLength(255)]
    public string? ShortenedUrl { get; set; }

    public IList<string>? Tags { get; set; }
}
