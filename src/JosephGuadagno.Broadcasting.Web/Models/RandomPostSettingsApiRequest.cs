namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API request payload for creating or updating random post settings.
/// </summary>
public class RandomPostSettingsApiRequest
{
    /// <summary>
    /// Gets or sets the social media platform identifier.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the cron expression.
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cutoff date.
    /// </summary>
    public DateTimeOffset? CutoffDate { get; set; }

    /// <summary>
    /// Gets or sets the excluded categories.
    /// </summary>
    public List<string> ExcludedCategories { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the settings are active.
    /// </summary>
    public bool IsActive { get; set; }
}
