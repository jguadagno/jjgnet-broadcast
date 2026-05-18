namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the current onboarding setup completion status for the authenticated user.
/// </summary>
public class SetupStatus
{
    /// <summary>Gets or sets whether the user has at least one collector configured.</summary>
    public bool HasCollector { get; set; }

    /// <summary>Gets or sets whether the user has at least one publisher enabled.</summary>
    public bool HasPublisher { get; set; }

    /// <summary>
    /// Gets or sets whether the user has message templates covering all of their configured publishers.
    /// Considered complete when no publishers are configured or when every configured publisher platform
    /// has at least one message template.
    /// </summary>
    public bool HasMessageTemplates { get; set; }

    /// <summary>Gets the list of publisher platform names that the user has enabled.</summary>
    public IReadOnlyList<string> ConfiguredPublisherPlatforms { get; set; } = [];

    /// <summary>Gets the list of configured publisher platforms that are missing message templates.</summary>
    public IReadOnlyList<string> MissingTemplatePlatforms { get; set; } = [];

    /// <summary>Gets whether all three onboarding steps are complete.</summary>
    public bool IsComplete => HasCollector && HasPublisher && HasMessageTemplates;
}
