namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// ViewModel for a role
/// </summary>
public class RoleViewModel
{
    /// <summary>
    /// The unique identifier for the role
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the role
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what this role provides
    /// </summary>
    public string? Description { get; set; }
}
