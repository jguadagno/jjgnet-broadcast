using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating a per-user random post schedule and filter configuration.
/// </summary>
public class CreateUserRandomPostSettingsRequest
{
    /// <summary>
    /// The social media platform identifier to publish random posts to.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The Cron expression that determines when the random post job should run.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// The oldest publication date eligible for random post selection, expressed as UTC.
    /// </summary>
    public DateTimeOffset? CutoffDate { get; set; }

    /// <summary>
    /// Categories to exclude from random post selection.
    /// </summary>
    public List<string>? ExcludedCategories { get; set; }

    /// <summary>
    /// Indicates whether this random post configuration is active.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request DTO for updating a per-user random post schedule and filter configuration.
/// Any null property is ignored so the existing persisted value is preserved.
/// </summary>
public class UpdateUserRandomPostSettingsRequest
{
    /// <summary>
    /// The social media platform identifier to publish random posts to.
    /// Leave null to keep the existing value.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The Cron expression that determines when the random post job should run.
    /// Leave null to keep the existing value.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// The oldest publication date eligible for random post selection, expressed as UTC.
    /// Leave null to keep the existing value.
    /// </summary>
    public DateTimeOffset? CutoffDate { get; set; }

    /// <summary>
    /// Categories to exclude from random post selection.
    /// Leave null to keep the existing value.
    /// </summary>
    public List<string>? ExcludedCategories { get; set; }

    /// <summary>
    /// Indicates whether this random post configuration is active.
    /// Leave null to keep the existing value.
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Response DTO for a per-user random post schedule and filter configuration.
/// </summary>
public class UserRandomPostSettingsResponse
{
    /// <summary>
    /// The unique identifier of this random post settings record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who owns this random post settings record.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// The social media platform identifier to publish random posts to.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The Cron expression that determines when the random post job should run.
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// The oldest publication date eligible for random post selection, expressed as UTC.
    /// </summary>
    public DateTimeOffset? CutoffDate { get; set; }

    /// <summary>
    /// Categories excluded from random post selection.
    /// </summary>
    public List<string> ExcludedCategories { get; set; } = [];

    /// <summary>
    /// Indicates whether this random post configuration is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time when this configuration was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// The date and time when this configuration was most recently updated.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

/// <summary>
/// Request DTO for creating a per-user event-to-dispatcher mapping.
/// </summary>
public class CreateUserEventDispatcherMappingRequest
{
    /// <summary>
    /// The event type to route.
    /// Supported values: <c>NewSyndicationFeedItem</c>, <c>NewYouTubeItem</c>,
    /// <c>NewSpeakingEngagement</c>, <c>RandomPost</c>, <c>ScheduledItem</c>.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [RegularExpression("^(NewSyndicationFeedItem|NewYouTubeItem|NewSpeakingEngagement|RandomPost|ScheduledItem)$")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// The social media platform identifier to route the event to.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Indicates whether this event dispatcher mapping is active.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request DTO for updating a per-user event-to-dispatcher mapping.
/// Any null property is ignored so the existing persisted value is preserved.
/// </summary>
public class UpdateUserEventDispatcherMappingRequest
{
    /// <summary>
    /// The event type to route.
    /// Supported values: <c>NewSyndicationFeedItem</c>, <c>NewYouTubeItem</c>,
    /// <c>NewSpeakingEngagement</c>, <c>RandomPost</c>, <c>ScheduledItem</c>.
    /// Leave null to keep the existing value.
    /// </summary>
    [RegularExpression("^(NewSyndicationFeedItem|NewYouTubeItem|NewSpeakingEngagement|RandomPost|ScheduledItem)$")]
    public string? EventType { get; set; }

    /// <summary>
    /// The social media platform identifier to route the event to.
    /// Leave null to keep the existing value.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Indicates whether this event dispatcher mapping is active.
    /// Leave null to keep the existing value.
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Response DTO for a per-user event-to-dispatcher mapping.
/// </summary>
public class UserEventDispatcherMappingResponse
{
    /// <summary>
    /// The unique identifier of this event dispatcher mapping record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who owns this event dispatcher mapping record.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// The event type routed by this mapping.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// The social media platform identifier to route the event to.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Indicates whether this event dispatcher mapping is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time when this configuration was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// The date and time when this configuration was most recently updated.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

