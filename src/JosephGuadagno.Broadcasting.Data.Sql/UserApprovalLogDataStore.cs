using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// Data store implementation for user approval log operations
/// </summary>
public class UserApprovalLogDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IUserApprovalLogDataStore
{
    public async Task<List<Domain.Models.UserApprovalLog>> GetByUserIdAsync(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        var dbLogs = await broadcastingContext.UserApprovalLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return mapper.Map<List<Domain.Models.UserApprovalLog>>(dbLogs);
    }

    public async Task<Domain.Models.UserApprovalLog> CreateAsync(Domain.Models.UserApprovalLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        var dbLog = mapper.Map<Models.UserApprovalLog>(log);
        dbLog.CreatedAt = DateTimeOffset.UtcNow;

        broadcastingContext.UserApprovalLogs.Add(dbLog);

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.UserApprovalLog>(dbLog);
        }

        throw new ApplicationException("Failed to create user approval log entry");
    }
}
