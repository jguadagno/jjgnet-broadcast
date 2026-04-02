using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for user approval operations
/// </summary>
public interface IUserApprovalManager
{
    /// <summary>
    /// Gets a user by their Entra object ID, creating them if they don't exist
    /// </summary>
    /// <param name="entraObjectId">The Entra object ID</param>
    /// <param name="displayName">The display name</param>
    /// <param name="email">The email address</param>
    /// <returns>The user</returns>
    Task<ApplicationUser> GetOrCreateUserAsync(string entraObjectId, string displayName, string email);

    /// <summary>
    /// Gets a user by their Entra object ID
    /// </summary>
    /// <param name="entraObjectId">The Entra object ID</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<ApplicationUser?> GetUserAsync(string entraObjectId);

    /// <summary>
    /// Gets a user by their integer ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<ApplicationUser?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Gets all users pending approval
    /// </summary>
    /// <returns>List of pending users</returns>
    Task<List<ApplicationUser>> GetPendingUsersAsync();

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>List of all users</returns>
    Task<List<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Gets users filtered by approval status at the database level
    /// </summary>
    /// <param name="status">The approval status to filter by</param>
    /// <returns>List of users with the specified status</returns>
    Task<List<ApplicationUser>> GetUsersByStatusAsync(ApprovalStatus status);

    /// <summary>
    /// Approves a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="adminUserId">The ID of the administrator approving</param>
    /// <returns>The updated user</returns>
    Task<ApplicationUser> ApproveUserAsync(int userId, int adminUserId);

    /// <summary>
    /// Rejects a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="adminUserId">The ID of the administrator rejecting</param>
    /// <param name="rejectionNotes">The reason for rejection (required)</param>
    /// <returns>The updated user</returns>
    Task<ApplicationUser> RejectUserAsync(int userId, int adminUserId, string rejectionNotes);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID</param>
    /// <param name="adminUserId">The ID of the administrator assigning the role</param>
    /// <returns>True if successful</returns>
    Task<bool> AssignRoleAsync(int userId, int roleId, int adminUserId);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID</param>
    /// <param name="adminUserId">The ID of the administrator removing the role</param>
    /// <returns>True if successful</returns>
    Task<bool> RemoveRoleAsync(int userId, int roleId, int adminUserId);

    /// <summary>
    /// Gets all roles for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of roles</returns>
    Task<List<Role>> GetUserRolesAsync(int userId);

    /// <summary>
    /// Gets all available roles
    /// </summary>
    /// <returns>List of all roles</returns>
    Task<List<Role>> GetAllRolesAsync();

    /// <summary>
    /// Gets the audit log for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of log entries</returns>
    Task<List<UserApprovalLog>> GetUserAuditLogAsync(int userId);
}
