using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents an application user with their approval status
/// </summary>
public class ApplicationUser
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The Microsoft Entra (Azure AD) object ID for this user
    /// </summary>
    [Required]
    public string EntraObjectId { get; set; } = null!;

    /// <summary>
    /// The display name of the user
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The email address of the user
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The approval status of the user
    /// </summary>
    [Required]
    public string ApprovalStatus { get; set; } = null!;

    /// <summary>
    /// Notes regarding the approval decision
    /// </summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>
    /// The date and time the user was created
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time the user was last updated
    /// </summary>
    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The roles assigned to this user
    /// </summary>
    public List<Role>? Roles { get; set; }
}
