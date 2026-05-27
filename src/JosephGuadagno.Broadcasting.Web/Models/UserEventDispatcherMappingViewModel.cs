using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for creating and editing per-user event dispatcher mappings.
/// </summary>
public class UserEventDispatcherMappingViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event type value.
    /// </summary>
    [Required]
    [Display(Name = "Event Type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display label for the event type.
    /// </summary>
    public string EventTypeDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon for the event type.
    /// </summary>
    public string EventTypeIcon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target social media platform identifier.
    /// </summary>
    [Display(Name = "Social Media Platform")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a social media platform.")]
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the display name for the selected platform.
    /// </summary>
    public string SocialMediaPlatformName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon for the selected platform.
    /// </summary>
    public string SocialMediaPlatformIcon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the mapping is active.
    /// </summary>
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }

    /// <summary>
    /// Gets or sets the available social media platform options.
    /// </summary>
    public List<SelectListItem> SocialMediaPlatforms { get; set; } = [];

    /// <summary>
    /// Gets or sets the available event type options.
    /// </summary>
    public List<SelectListItem> EventTypes { get; set; } = [];
}
