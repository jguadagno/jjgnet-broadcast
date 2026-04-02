using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// ViewModel for managing user roles
/// </summary>
public class ManageRolesViewModel
{
    /// <summary>
    /// The user whose roles are being managed
    /// </summary>
    public ApplicationUserViewModel User { get; set; } = null!;

    /// <summary>
    /// Roles currently assigned to the user
    /// </summary>
    public IList<Role> CurrentRoles { get; set; } = new List<Role>();

    /// <summary>
    /// All available roles in the system
    /// </summary>
    public IList<Role> AvailableRoles { get; set; } = new List<Role>();
}
