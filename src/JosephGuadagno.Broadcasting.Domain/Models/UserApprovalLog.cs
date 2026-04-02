using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents an audit log entry for user approval actions
/// </summary>
public class UserApprovalLog
{
    /// <summary>
    /// The unique identifier of the log entry
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The ID of the user this action was performed on
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The ID of the administrator who performed this action (null for system actions)
    /// </summary>
    public int? AdminUserId { get; set; }

    /// <summary>
    /// The action that was performed
    /// </summary>
    [Required]
    public string Action { get; set; }

    /// <summary>
    /// Notes regarding this action
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// The date and time this action was performed
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The user this action was performed on
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// The administrator who performed this action
    /// </summary>
    public ApplicationUser? AdminUser { get; set; }
}
