using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for role operations
/// </summary>
public interface IRoleDataStore
{
    /// <summary>
    /// Gets all roles
    /// </summary>
    /// <returns>List of all roles</returns>
    Task<List<Role>> GetAllAsync();

    /// <summary>
    /// Gets a role by its ID
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <returns>The role if found, otherwise null</returns>
    Task<Role?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a role by its name
    /// </summary>
    /// <param name="name">The role name</param>
    /// <returns>The role if found, otherwise null</returns>
    Task<Role?> GetByNameAsync(string name);

    /// <summary>
    /// Gets all roles assigned to a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of roles assigned to the user</returns>
    Task<List<Role>> GetRolesForUserAsync(int userId);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> AssignRoleToUserAsync(int userId, int roleId);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="roleId">The role ID</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
}
