using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

/// <summary>
/// EF Core entity representing a per-user random post schedule and filtering configuration.
/// </summary>
public class UserRandomPostSettings
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Entra Object ID of the user who owns this configuration.
    /// </summary>
    [MaxLength(36)]
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the social media platform identifier to publish to.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the social media platform.
    /// </summary>
    public SocialMediaPlatform? SocialMediaPlatform { get; set; }

    /// <summary>
    /// Gets or sets the cron expression that controls when this random post runs.
    /// </summary>
    [MaxLength(100)]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the oldest publication date eligible for random post selection.
    /// </summary>
    public DateTimeOffset? CutoffDate { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated categories excluded from random post selection.
    /// </summary>
    public string? ExcludedCategories { get; set; }

    /// <summary>
    /// Gets or sets whether this random post schedule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when this configuration was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}
