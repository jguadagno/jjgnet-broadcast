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
    /// Gets or sets whether all required message template combinations (publisher × collector type)
    /// exist for this user. Considered complete when the user has no publishers or no collectors,
    /// or when every required (publisher, messageType) pair has a template.
    /// </summary>
    public bool HasMessageTemplates { get; set; }

    /// <summary>Gets the list of publisher platform names that the user has enabled.</summary>
    public IReadOnlyList<string> ConfiguredPublisherPlatforms { get; set; } = [];

    /// <summary>Gets the message type constants for each collector type the user has configured.</summary>
    public IReadOnlyList<string> ConfiguredCollectorTypes { get; set; } = [];

    /// <summary>
    /// Gets the (platform, messageType) pairs that are required but do not yet have a template.
    /// Derived from the intersection of <see cref="ConfiguredPublisherPlatforms"/> ×
    /// <see cref="ConfiguredCollectorTypes"/>.
    /// </summary>
    public IReadOnlyList<MissingTemplateKey> MissingTemplatePairs { get; set; } = [];

    /// <summary>
    /// Gets the distinct publisher platform names that are missing at least one required template.
    /// Derived from <see cref="MissingTemplatePairs"/> for display convenience.
    /// </summary>
    public IReadOnlyList<string> MissingTemplatePlatforms { get; set; } = [];

    /// <summary>Gets whether all three onboarding steps are complete.</summary>
    public bool IsComplete => HasCollector && HasPublisher && HasMessageTemplates;
}
