using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for a user collector YouTube channel.
/// </summary>
public class UserCollectorYouTubeChannelViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Channel ID is required.")]
    [StringLength(50, ErrorMessage = "Channel ID cannot exceed 50 characters.")]
    [Display(Name = "YouTube Channel ID")]
    public string ChannelId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(255, ErrorMessage = "Display name cannot exceed 255 characters.")]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset LastUpdatedOn { get; set; }

    public bool IsManagedBySiteAdmin { get; set; }
}
