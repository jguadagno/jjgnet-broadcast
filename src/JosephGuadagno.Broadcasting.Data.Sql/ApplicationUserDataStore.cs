using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// Data store implementation for application user operations
/// </summary>
public class ApplicationUserDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IApplicationUserDataStore
{
    public async Task<Domain.Models.ApplicationUser?> GetByEntraObjectIdAsync(string entraObjectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);

        var dbUser = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId);

        return dbUser is null ? null : mapper.Map<Domain.Models.ApplicationUser>(dbUser);
    }

    public async Task<Domain.Models.ApplicationUser?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(id));
        }

        var dbUser = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        return dbUser is null ? null : mapper.Map<Domain.Models.ApplicationUser>(dbUser);
    }

    public async Task<List<Domain.Models.ApplicationUser>> GetAllAsync()
    {
        var dbUsers = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();

        return mapper.Map<List<Domain.Models.ApplicationUser>>(dbUsers);
    }

    public async Task<List<Domain.Models.ApplicationUser>> GetByApprovalStatusAsync(string approvalStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalStatus);

        var dbUsers = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.ApprovalStatus == approvalStatus)
            .ToListAsync();

        return mapper.Map<List<Domain.Models.ApplicationUser>>(dbUsers);
    }

    public async Task<Domain.Models.ApplicationUser> CreateAsync(Domain.Models.ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var dbUser = mapper.Map<Models.ApplicationUser>(user);
        dbUser.CreatedAt = DateTimeOffset.UtcNow;
        dbUser.UpdatedAt = DateTimeOffset.UtcNow;

        broadcastingContext.ApplicationUsers.Add(dbUser);

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.ApplicationUser>(dbUser);
        }

        throw new ApplicationException("Failed to create user");
    }

    public async Task<Domain.Models.ApplicationUser> UpdateAsync(Domain.Models.ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var dbUser = await broadcastingContext.ApplicationUsers.FindAsync(user.Id);
        if (dbUser is null)
        {
            throw new ApplicationException($"User with id '{user.Id}' not found");
        }

        // Update properties
        dbUser.DisplayName = user.DisplayName;
        dbUser.Email = user.Email;
        dbUser.ApprovalStatus = user.ApprovalStatus;
        dbUser.ApprovalNotes = user.ApprovalNotes;
        dbUser.UpdatedAt = DateTimeOffset.UtcNow;

        broadcastingContext.Entry(dbUser).State = EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.ApplicationUser>(dbUser);
        }

        throw new ApplicationException("Failed to update user");
    }
}
