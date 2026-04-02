using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Represents a role that can be assigned to users
/// </summary>
public class Role
{
    /// <summary>
    /// The unique identifier of the role
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The name of the role
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// A description of the role and its permissions
    /// </summary>
    public string? Description { get; set; }
}
