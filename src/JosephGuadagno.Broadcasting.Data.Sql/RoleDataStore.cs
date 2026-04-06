using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class RoleDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IRoleDataStore
{
    public async Task<List<Domain.Models.Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbRoles = await broadcastingContext.Roles.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.Role>>(dbRoles);
    }

    public async Task<Domain.Models.Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentException("Role ID must be greater than 0", nameof(id));

        var dbRole = await broadcastingContext.Roles.FindAsync(new object[] { id }, cancellationToken);
        return dbRole is null ? null : mapper.Map<Domain.Models.Role>(dbRole);
    }

    public async Task<Domain.Models.Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var dbRole = await broadcastingContext.Roles
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        return dbRole is null ? null : mapper.Map<Domain.Models.Role>(dbRole);
    }

    public async Task<List<Domain.Models.Role>> GetRolesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));

        var dbRoles = await broadcastingContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Domain.Models.Role>>(dbRoles);
    }

    public async Task<bool> AssignRoleToUserAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        if (roleId <= 0) throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));

        var existing = await broadcastingContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (existing is not null) return true;

        var userRole = new Models.UserRole { UserId = userId, RoleId = roleId };
        broadcastingContext.UserRoles.Add(userRole);
        return await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        if (roleId <= 0) throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));

        var userRole = await broadcastingContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole is null) return true;

        broadcastingContext.UserRoles.Remove(userRole);
        return await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
    }
}
