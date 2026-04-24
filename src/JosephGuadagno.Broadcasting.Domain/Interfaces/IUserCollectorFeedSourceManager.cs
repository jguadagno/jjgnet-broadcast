using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user RSS/Atom/JSON feed collector configurations
/// </summary>
public interface IUserCollectorFeedSourceManager
{
    /// <summary>
    /// Retrieves all feed source configurations for a specific user
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of feed source configurations for the user</returns>
    Task<List<UserCollectorFeedSource>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a feed source configuration by its ID
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feed source configuration if found, otherwise null</returns>
    Task<UserCollectorFeedSource?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all active feed source configurations across all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all active feed source configurations</returns>
    Task<List<UserCollectorFeedSource>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates or updates a feed source configuration
    /// </summary>
    /// <param name="config">The configuration to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved configuration if successful, otherwise null</returns>
    Task<UserCollectorFeedSource?> SaveAsync(UserCollectorFeedSource config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a feed source configuration
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found or error occurred</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);
}
