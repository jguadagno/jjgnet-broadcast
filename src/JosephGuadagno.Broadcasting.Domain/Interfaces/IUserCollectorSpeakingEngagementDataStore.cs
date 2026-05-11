using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for managing per-user speaking engagements file collector configurations
/// </summary>
public interface IUserCollectorSpeakingEngagementDataStore
{
    /// <summary>
    /// Retrieves all speaking engagement configurations for a specific user
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of speaking engagement configurations for the user</returns>
    Task<List<UserCollectorSpeakingEngagement>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a speaking engagement configuration by its ID
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The speaking engagement configuration if found, otherwise null</returns>
    Task<UserCollectorSpeakingEngagement?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active speaking engagement configurations across all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all active speaking engagement configurations</returns>
    Task<List<UserCollectorSpeakingEngagement>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a speaking engagement configuration
    /// </summary>
    /// <param name="config">The configuration to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved configuration if successful, otherwise null</returns>
    Task<UserCollectorSpeakingEngagement?> SaveAsync(UserCollectorSpeakingEngagement config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a speaking engagement configuration
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found or error occurred</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);

    Task<PagedResult<UserCollectorSpeakingEngagement>> GetAllAsync(string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}
