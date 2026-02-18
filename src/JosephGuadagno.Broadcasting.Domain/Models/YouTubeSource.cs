using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class YouTubeSource
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string VideoId { get; set; }

    [Required]
    [StringLength(255)]
    public string Author { get; set; }

    [Required]
    [StringLength(512)]
    public string Title { get; set; }

    [StringLength(255)]
    public string? ShortenedUrl { get; set; }

    public string? Tags { get; set; }

    [Required]
    [Url]
    public string Url { get; set; }

    [Required]
    public DateTimeOffset PublicationDate { get; set; }

    [Required]
    public DateTimeOffset AddedOn { get; set; }

    public DateTimeOffset? ItemLastUpdatedOn { get; set; }

    [Required]
    public DateTimeOffset LastUpdatedOn { get; set; }
}