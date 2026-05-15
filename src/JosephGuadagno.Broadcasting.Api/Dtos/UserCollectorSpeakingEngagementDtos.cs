using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a per-user speaking engagement collector configuration.
/// Used by the user collector speaking engagements endpoints.
/// </summary>
public class UserCollectorSpeakingEngagementRequest
{
    /// <summary>
    /// The path or URL to the file containing the user's speaking engagement data
    /// (e.g., a remote JSON or XML feed). Maximum 2048 characters.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [StringLength(2048)]
    public string SpeakingEngagementsFile { get; set; } = string.Empty;

    /// <summary>
    /// The friendly display name for this speaking engagement collector,
    /// used to identify it in the UI. Maximum 255 characters.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [StringLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this speaking engagement collector is currently active.
    /// Defaults to <c>true</c>; set to <c>false</c> to disable without deleting the record.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response DTO for a per-user speaking engagement collector configuration, returned by the
/// user collector speaking engagements endpoints.
/// </summary>
public class UserCollectorSpeakingEngagementResponse
{
    /// <summary>
    /// The unique identifier of this speaking engagement collector configuration record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The path or URL to the file containing the user's speaking engagement data.
    /// </summary>
    public string SpeakingEngagementsFile { get; set; } = string.Empty;

    /// <summary>
    /// The friendly display name for this speaking engagement collector.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this speaking engagement collector is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time when this configuration was first created, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// The date and time when this configuration was most recently updated, stored as <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who owns this configuration.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
}
