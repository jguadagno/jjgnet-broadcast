namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// ViewModel for displaying categorized user lists on the Admin/Users page
/// </summary>
public class UserListViewModel
{
    /// <summary>
    /// Users pending approval
    /// </summary>
    public List<ApplicationUserViewModel> PendingUsers { get; set; } = new();

    /// <summary>
    /// Users who have been approved
    /// </summary>
    public List<ApplicationUserViewModel> ApprovedUsers { get; set; } = new();

    /// <summary>
    /// Users who have been rejected
    /// </summary>
    public List<ApplicationUserViewModel> RejectedUsers { get; set; } = new();
}
