using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// ViewModel for rejecting a user with required notes
/// </summary>
public class RejectUserViewModel
{
    /// <summary>
    /// The ID of the user to reject
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The reason for rejection (required)
    /// </summary>
    [Required(ErrorMessage = "Rejection notes are required.")]
    public string RejectionNotes { get; set; } = null!;
}
