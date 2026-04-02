using JosephGuadagno.Broadcasting.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for account-related pages (approval status, etc.)
/// </summary>
public class AccountController : Controller
{
    /// <summary>
    /// Displays the pending approval page for users awaiting administrator approval
    /// </summary>
    /// <returns>The pending approval view</returns>
    [AllowAnonymous]
    public IActionResult PendingApproval()
    {
        return View();
    }

    /// <summary>
    /// Displays the rejection page for users whose registration was rejected
    /// </summary>
    /// <returns>The rejected view</returns>
    [AllowAnonymous]
    public IActionResult Rejected()
    {
        // Try to get approval notes from the user's claims
        var approvalNotesClaim = User.FindFirst(ApplicationClaimTypes.ApprovalNotes);
        if (approvalNotesClaim != null)
        {
            ViewBag.ApprovalNotes = approvalNotesClaim.Value;
        }
        
        return View();
    }
}
