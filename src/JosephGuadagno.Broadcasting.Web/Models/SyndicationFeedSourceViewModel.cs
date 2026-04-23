using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for a syndication feed source
/// </summary>
public class SyndicationFeedSourceViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Feed Identifier")]
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

    [StringLength(255)]
    [Display(Name = "Shortened URL")]
    public string? ShortenedUrl { get; set; }

    [Required]
    [Display(Name = "Publication Date")]
    public DateTimeOffset PublicationDate { get; set; }

    public string? Tags { get; set; }

    [Display(Name = "Added On")]
    public DateTimeOffset AddedOn { get; set; }

    [Display(Name = "Last Updated")]
    public DateTimeOffset LastUpdatedOn { get; set; }
}
