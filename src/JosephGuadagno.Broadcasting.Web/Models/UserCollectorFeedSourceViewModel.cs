using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for a user collector feed source.
/// </summary>
public class UserCollectorFeedSourceViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Feed URL is required.")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    [StringLength(2048, ErrorMessage = "Feed URL cannot exceed 2048 characters.")]
    [Display(Name = "Feed URL")]
    public string FeedUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(255, ErrorMessage = "Display name cannot exceed 255 characters.")]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset LastUpdatedOn { get; set; }

    public bool IsManagedBySiteAdmin { get; set; }
}
