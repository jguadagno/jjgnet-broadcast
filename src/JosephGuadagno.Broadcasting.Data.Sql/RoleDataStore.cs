using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// Data store implementation for role operations
/// </summary>
public class RoleDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IRoleDataStore
{
    public async Task<List<Domain.Models.Role>> GetAllAsync()
    {
        var dbRoles = await broadcastingContext.Roles.ToListAsync();
        return mapper.Map<List<Domain.Models.Role>>(dbRoles);
    }

    public async Task<Domain.Models.Role?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Role ID must be greater than 0", nameof(id));
        }

        var dbRole = await broadcastingContext.Roles.FindAsync(id);
        return dbRole is null ? null : mapper.Map<Domain.Models.Role>(dbRole);
    }

    public async Task<Domain.Models.Role?> GetByNameAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var dbRole = await broadcastingContext.Roles
            .FirstOrDefaultAsync(r => r.Name == name);

        return dbRole is null ? null : mapper.Map<Domain.Models.Role>(dbRole);
    }

    public async Task<List<Domain.Models.Role>> GetRolesForUserAsync(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        var dbRoles = await broadcastingContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync();

        return mapper.Map<List<Domain.Models.Role>>(dbRoles);
    }

    public async Task<bool> AssignRoleToUserAsync(int userId, int roleId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        if (roleId <= 0)
        {
            throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));
        }

        // Check if assignment already exists
        var existing = await broadcastingContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (existing is not null)
        {
            return true; // Already assigned
        }

        var userRole = new Models.UserRole
        {
            UserId = userId,
            RoleId = roleId
        };

        broadcastingContext.UserRoles.Add(userRole);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        if (roleId <= 0)
        {
            throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));
        }

        var userRole = await broadcastingContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole is null)
        {
            return true; // Already removed
        }

        broadcastingContext.UserRoles.Remove(userRole);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }
}
