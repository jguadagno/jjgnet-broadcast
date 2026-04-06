using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class ApplicationUserDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IApplicationUserDataStore
{
    public async Task<Domain.Models.ApplicationUser?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);

        var dbUser = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, cancellationToken);

        return dbUser is null ? null : mapper.Map<Domain.Models.ApplicationUser>(dbUser);
    }

    public async Task<Domain.Models.ApplicationUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(id));

        var dbUser = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return dbUser is null ? null : mapper.Map<Domain.Models.ApplicationUser>(dbUser);
    }

    public async Task<List<Domain.Models.ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbUsers = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Domain.Models.ApplicationUser>>(dbUsers);
    }

    public async Task<List<Domain.Models.ApplicationUser>> GetByApprovalStatusAsync(string approvalStatus, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalStatus);

        var dbUsers = await broadcastingContext.ApplicationUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.ApprovalStatus == approvalStatus)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Domain.Models.ApplicationUser>>(dbUsers);
    }

    public async Task<Domain.Models.ApplicationUser> CreateAsync(Domain.Models.ApplicationUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var dbUser = mapper.Map<Models.ApplicationUser>(user);
        dbUser.CreatedAt = DateTimeOffset.UtcNow;
        dbUser.UpdatedAt = DateTimeOffset.UtcNow;

        broadcastingContext.ApplicationUsers.Add(dbUser);

        var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.ApplicationUser>(dbUser);
        }

        throw new ApplicationException("Failed to create user");
    }

    public async Task<Domain.Models.ApplicationUser> UpdateAsync(Domain.Models.ApplicationUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var dbUser = await broadcastingContext.ApplicationUsers.FindAsync(new object[] { user.Id }, cancellationToken);
        if (dbUser is null) throw new ApplicationException($"User with id '{user.Id}' not found");

        dbUser.DisplayName = user.DisplayName;
        dbUser.Email = user.Email;
        dbUser.ApprovalStatus = user.ApprovalStatus;
        dbUser.ApprovalNotes = user.ApprovalNotes;
        dbUser.UpdatedAt = DateTimeOffset.UtcNow;

        broadcastingContext.Entry(dbUser).State = EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.ApplicationUser>(dbUser);
        }

        throw new ApplicationException("Failed to update user");
    }
}
