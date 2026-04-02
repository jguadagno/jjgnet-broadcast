using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents the assignment of a role to a user
/// </summary>
public class UserRole
{
    /// <summary>
    /// The user ID
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The role ID
    /// </summary>
    [Required]
    public int RoleId { get; set; }

    /// <summary>
    /// The user associated with this assignment
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// The role associated with this assignment
    /// </summary>
    public Role? Role { get; set; }
}
