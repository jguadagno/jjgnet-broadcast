using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for a per-user scheduled item collector configuration.
/// </summary>
public class UserCollectorScheduledItemViewModel
{
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
