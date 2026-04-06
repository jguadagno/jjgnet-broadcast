using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class UserApprovalLogDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IUserApprovalLogDataStore
{
    public async Task<List<Domain.Models.UserApprovalLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));

        var dbLogs = await broadcastingContext.UserApprovalLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Domain.Models.UserApprovalLog>>(dbLogs);
    }

    public async Task<Domain.Models.UserApprovalLog> CreateAsync(Domain.Models.UserApprovalLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);

        var dbLog = mapper.Map<Models.UserApprovalLog>(log);
        dbLog.CreatedAt = DateTimeOffset.UtcNow;

        broadcastingContext.UserApprovalLogs.Add(dbLog);

        var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.UserApprovalLog>(dbLog);
        }

        throw new ApplicationException("Failed to create user approval log entry");
    }
}
