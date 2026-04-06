using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for user approval operations
/// </summary>
public interface IUserApprovalManager
{
    Task<ApplicationUser> GetOrCreateUserAsync(string entraObjectId, string displayName, string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetUserAsync(string entraObjectId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> GetPendingUsersAsync(CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> GetUsersByStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default);
    Task<ApplicationUser> ApproveUserAsync(int userId, int adminUserId, CancellationToken cancellationToken = default);
    Task<ApplicationUser> RejectUserAsync(int userId, int adminUserId, string rejectionNotes, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleAsync(int userId, int roleId, int adminUserId, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleAsync(int userId, int roleId, int adminUserId, CancellationToken cancellationToken = default);
    Task<List<Role>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<List<UserApprovalLog>> GetUserAuditLogAsync(int userId, CancellationToken cancellationToken = default);
}
