using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for creating and editing per-user random post settings.
/// </summary>
public class UserRandomPostSettingsViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

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
    /// Gets or sets the cron expression that controls scheduling.
    /// </summary>
    [Required]
    [Display(Name = "Cron Expression")]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the browser-local cutoff date value used by the datetime-local input.
    /// </summary>
    [Display(Name = "Cutoff Date")]
    public string? CutoffDateLocal { get; set; }

    /// <summary>
    /// Gets or sets the UTC cutoff date value submitted by the browser.
    /// </summary>
    public string? CutoffDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the excluded categories as comma-separated text.
    /// </summary>
    [Display(Name = "Excluded Categories")]
    public string ExcludedCategoriesText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the setting is active.
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
}
