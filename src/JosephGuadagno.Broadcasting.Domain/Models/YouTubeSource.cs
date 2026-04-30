using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class YouTubeSource
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string VideoId { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string Author { get; set; } = null!;

    [Required]
    [StringLength(512)]
    public string Title { get; set; } = null!;

    [StringLength(255)]
    public string? ShortenedUrl { get; set; }

    public IList<string> Tags { get; set; } = [];

    [Required]
    [Url]
    public string Url { get; set; } = null!;

    [Required]
    public DateTimeOffset PublicationDate { get; set; }

    [Required]
    public DateTimeOffset AddedOn { get; set; }

    public DateTimeOffset? ItemLastUpdatedOn { get; set; }

    [Required]
    public DateTimeOffset LastUpdatedOn { get; set; }

    [Required]
    [StringLength(36)]
    public string CreatedByEntraOid { get; set; } = null!;
}