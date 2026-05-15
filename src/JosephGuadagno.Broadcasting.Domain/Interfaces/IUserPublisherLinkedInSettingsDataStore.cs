using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for managing per-user LinkedIn publisher settings
/// </summary>
public interface IUserPublisherLinkedInSettingsDataStore
{
    /// <summary>
    /// Retrieves the LinkedIn settings for a specific user
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The settings if found, otherwise null</returns>
    Task<UserPublisherLinkedInSettings?> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves LinkedIn settings by their record ID
    /// </summary>
    /// <param name="id">The record ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The settings if found, otherwise null</returns>
    Task<UserPublisherLinkedInSettings?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates LinkedIn settings for a user
    /// </summary>
    /// <param name="settings">The settings to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved settings if successful, otherwise null</returns>
    Task<UserPublisherLinkedInSettings?> SaveAsync(UserPublisherLinkedInSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes LinkedIn settings for a user
    /// </summary>
    /// <param name="id">The record ID</param>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found or an error occurred</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);
}
