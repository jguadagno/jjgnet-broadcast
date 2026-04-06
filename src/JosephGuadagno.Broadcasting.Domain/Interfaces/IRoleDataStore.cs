using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for role operations
/// </summary>
public interface IRoleDataStore
{
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Role>> GetRolesForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleToUserAsync(int userId, int roleId, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken cancellationToken = default);
}
