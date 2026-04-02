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
    public IList<RoleViewModel> CurrentRoles { get; set; } = new List<RoleViewModel>();

    /// <summary>
    /// All available roles in the system
    /// </summary>
    public IList<RoleViewModel> AvailableRoles { get; set; } = new List<RoleViewModel>();
}
