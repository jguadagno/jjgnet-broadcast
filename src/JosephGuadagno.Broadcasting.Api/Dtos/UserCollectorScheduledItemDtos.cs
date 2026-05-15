using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a per-user scheduled item collector configuration.
/// Used by the user collector scheduled items endpoints.
/// </summary>
public class UserCollectorScheduledItemRequest
{
    /// <summary>
    /// The friendly display name for this scheduled item collector configuration,
    /// used to identify it in the UI. Maximum 255 characters.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    [StringLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this scheduled item collector configuration is currently active.
    /// Defaults to <c>true</c>; set to <c>false</c> to disable without deleting the record.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response DTO for a per-user scheduled item collector configuration, returned by the
/// user collector scheduled items endpoints.
/// </summary>
public class UserCollectorScheduledItemResponse
{
    /// <summary>
    /// The unique identifier of this scheduled item collector configuration record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The friendly display name for this scheduled item collector configuration.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this scheduled item collector configuration is currently active.
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
