using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for user approval log operations
/// </summary>
public interface IUserApprovalLogDataStore
{
    /// <summary>
    /// Gets all log entries for a user
    /// </summary>
    Task<List<UserApprovalLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new log entry
    /// </summary>
    Task<UserApprovalLog> CreateAsync(UserApprovalLog log, CancellationToken cancellationToken = default);
}
