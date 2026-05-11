using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for a user collector speaking engagement.
/// </summary>
public class UserCollectorSpeakingEngagementViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Speaking engagements file URL is required.")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    [StringLength(2048, ErrorMessage = "URL cannot exceed 2048 characters.")]
    [Display(Name = "Speaking Engagements File URL")]
    public string SpeakingEngagementsFile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(255, ErrorMessage = "Display name cannot exceed 255 characters.")]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset LastUpdatedOn { get; set; }

    public bool IsManagedBySiteAdmin { get; set; }
}
