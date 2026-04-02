using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    private const string EntraObjectIdClaimType = ApplicationClaimTypes.EntraObjectId;

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
            var viewModel = new UserListViewModel
            {
                PendingUsers = _mapper.Map<List<ApplicationUserViewModel>>(
                    await _userApprovalManager.GetUsersByStatusAsync(ApprovalStatus.Pending)),
                ApprovedUsers = _mapper.Map<List<ApplicationUserViewModel>>(
                    await _userApprovalManager.GetUsersByStatusAsync(ApprovalStatus.Approved)),
                RejectedUsers = _mapper.Map<List<ApplicationUserViewModel>>(
                    await _userApprovalManager.GetUsersByStatusAsync(ApprovalStatus.Rejected))
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

    /// <summary>
    /// Displays the role management page for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>The role management view</returns>
    [HttpGet]
    public async Task<IActionResult> ManageRoles(int userId)
    {
        var user = await _userApprovalManager.GetUserByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Users");
        }

        var currentRoles = await _userApprovalManager.GetUserRolesAsync(userId);
        var allRoles = await _userApprovalManager.GetAllRolesAsync();
        var currentRoleIds = currentRoles.Select(r => r.Id).ToHashSet();

        var viewModel = new ManageRolesViewModel
        {
            User = _mapper.Map<ApplicationUserViewModel>(user),
            CurrentRoles = currentRoles,
            AvailableRoles = allRoles.Where(r => !currentRoleIds.Contains(r.Id)).ToList()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID to assign</param>
    /// <returns>Redirects to ManageRoles</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(int userId, int roleId)
    {
        try
        {
            var adminUserId = await GetCurrentUserIdAsync();
            if (adminUserId == null)
            {
                TempData["ErrorMessage"] = "Could not identify current administrator.";
                return RedirectToAction("Users");
            }

            await _userApprovalManager.AssignRoleAsync(userId, roleId, adminUserId.Value);
            TempData["SuccessMessage"] = "Role assigned successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
            TempData["ErrorMessage"] = "Failed to assign role.";
        }

        return RedirectToAction("ManageRoles", new { userId });
    }

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID to remove</param>
    /// <returns>Redirects to ManageRoles</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(int userId, int roleId)
    {
        try
        {
            var adminUserId = await GetCurrentUserIdAsync();
            if (adminUserId == null)
            {
                TempData["ErrorMessage"] = "Could not identify current administrator.";
                return RedirectToAction("Users");
            }

            await _userApprovalManager.RemoveRoleAsync(userId, roleId, adminUserId.Value);
            TempData["SuccessMessage"] = "Role removed successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
            TempData["ErrorMessage"] = "Failed to remove role.";
        }

        return RedirectToAction("ManageRoles", new { userId });
    }
}
