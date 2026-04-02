namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// ViewModel for displaying application user information
/// </summary>
public class ApplicationUserViewModel
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public int Id { get; set; }

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
    public string ApprovalStatus { get; set; } = null!;

    /// <summary>
    /// Notes regarding the approval decision
    /// </summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>
    /// The date and time the user was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
