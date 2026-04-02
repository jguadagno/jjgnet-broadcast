using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for administrative functions (user approval, role management)
/// </summary>
[Authorize(Policy = "RequireAdministrator")]
public class AdminController : Controller
{
    private readonly IUserApprovalManager _userApprovalManager;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminController> _logger;
    private const string EntraObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// Constructor for the AdminController
    /// </summary>
    /// <param name="userApprovalManager">The user approval manager</param>
    /// <param name="mapper">The mapper service</param>
    /// <param name="logger">The logger</param>
    public AdminController(
        IUserApprovalManager userApprovalManager,
        IMapper mapper,
        ILogger<AdminController> logger)
    {
        _userApprovalManager = userApprovalManager;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Displays all users categorized by approval status
    /// </summary>
    /// <returns>The users list view</returns>
    public async Task<IActionResult> Users()
    {
        try
        {
            var allUsers = await _userApprovalManager.GetAllUsersAsync();
            
            var viewModel = new UserListViewModel
            {
                PendingUsers = _mapper.Map<List<ApplicationUserViewModel>>(
                    allUsers.Where(u => u.ApprovalStatus == ApprovalStatus.Pending.ToString()).ToList()),
                ApprovedUsers = _mapper.Map<List<ApplicationUserViewModel>>(
                    allUsers.Where(u => u.ApprovalStatus == ApprovalStatus.Approved.ToString()).ToList()),
                RejectedUsers = _mapper.Map<List<ApplicationUserViewModel>>(
                    allUsers.Where(u => u.ApprovalStatus == ApprovalStatus.Rejected.ToString()).ToList())
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user list");
            TempData["ErrorMessage"] = "Failed to retrieve user list.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Approves a pending user
    /// </summary>
    /// <param name="userId">The ID of the user to approve</param>
    /// <returns>Redirects to the Users page</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveUser(int userId)
    {
        try
        {
            var adminUserId = await GetCurrentUserIdAsync();
            if (adminUserId == null)
            {
                TempData["ErrorMessage"] = "Could not identify current administrator.";
                return RedirectToAction("Users");
            }

            await _userApprovalManager.ApproveUserAsync(userId, adminUserId.Value);
            TempData["SuccessMessage"] = "User approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving user {UserId}", userId);
            TempData["ErrorMessage"] = "Failed to approve user.";
        }

        return RedirectToAction("Users");
    }

    /// <summary>
    /// Rejects a pending user with required notes
    /// </summary>
    /// <param name="userId">The ID of the user to reject</param>
    /// <param name="rejectionNotes">The reason for rejection</param>
    /// <returns>Redirects to the Users page</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectUser(int userId, string rejectionNotes)
    {
        // Validate rejection notes
        if (string.IsNullOrWhiteSpace(rejectionNotes))
        {
            TempData["ErrorMessage"] = "Rejection notes are required.";
            return RedirectToAction("Users");
        }

        try
        {
            var adminUserId = await GetCurrentUserIdAsync();
            if (adminUserId == null)
            {
                TempData["ErrorMessage"] = "Could not identify current administrator.";
                return RedirectToAction("Users");
            }

            await _userApprovalManager.RejectUserAsync(userId, adminUserId.Value, rejectionNotes);
            TempData["SuccessMessage"] = "User rejected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting user {UserId}", userId);
            TempData["ErrorMessage"] = "Failed to reject user.";
        }

        return RedirectToAction("Users");
    }

    /// <summary>
    /// Gets the current user's ID from their Entra object ID claim
    /// </summary>
    /// <returns>The user ID, or null if not found</returns>
    private async Task<int?> GetCurrentUserIdAsync()
    {
        var objectIdClaim = User.FindFirst(EntraObjectIdClaimType);
        if (objectIdClaim == null)
        {
            _logger.LogWarning("Entra object ID claim not found for current user");
            return null;
        }

        var user = await _userApprovalManager.GetUserAsync(objectIdClaim.Value);
        return user?.Id;
    }
}
