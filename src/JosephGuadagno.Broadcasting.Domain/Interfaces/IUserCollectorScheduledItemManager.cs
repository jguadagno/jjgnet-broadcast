using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user scheduled item publisher configurations
/// </summary>
public interface IUserCollectorScheduledItemManager
{
    /// <summary>
    /// Retrieves all scheduled item configurations for a specific user
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scheduled item configurations for the user</returns>
    Task<List<UserCollectorScheduledItem>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a scheduled item configuration by its ID
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scheduled item configuration if found, otherwise null</returns>
    Task<UserCollectorScheduledItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active scheduled item configurations across all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all active scheduled item configurations</returns>
    Task<List<UserCollectorScheduledItem>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a scheduled item configuration
    /// </summary>
    /// <param name="config">The configuration to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved configuration if successful, otherwise null</returns>
    Task<UserCollectorScheduledItem?> SaveAsync(UserCollectorScheduledItem config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scheduled item configuration
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found or error occurred</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);

    Task<PagedResult<UserCollectorScheduledItem>> GetAllAsync(string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}
