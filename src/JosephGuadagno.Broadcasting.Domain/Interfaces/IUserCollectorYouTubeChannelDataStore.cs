using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for managing per-user YouTube channel collector configurations
/// </summary>
public interface IUserCollectorYouTubeChannelDataStore
{
    /// <summary>
    /// Retrieves all YouTube channel configurations for a specific user
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of YouTube channel configurations for the user</returns>
    Task<List<UserCollectorYouTubeChannel>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a YouTube channel configuration by its ID
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The YouTube channel configuration if found, otherwise null</returns>
    Task<UserCollectorYouTubeChannel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all active YouTube channel configurations across all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all active YouTube channel configurations</returns>
    Task<List<UserCollectorYouTubeChannel>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates or updates a YouTube channel configuration
    /// </summary>
    /// <param name="config">The configuration to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved configuration if successful, otherwise null</returns>
    Task<UserCollectorYouTubeChannel?> SaveAsync(UserCollectorYouTubeChannel config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a YouTube channel configuration
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found or error occurred</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);
}
