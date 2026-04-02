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
    /// <param name="userId">The user ID</param>
    /// <returns>List of log entries for the user</returns>
    Task<List<UserApprovalLog>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Creates a new log entry
    /// </summary>
    /// <param name="log">The log entry to create</param>
    /// <returns>The created log entry</returns>
    Task<UserApprovalLog> CreateAsync(UserApprovalLog log);
}
