using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for managing per-user Facebook publisher settings
/// </summary>
public interface IUserPlatformFacebookSettingsDataStore
{
    /// <summary>
    /// Retrieves the Facebook settings for a specific user
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The settings if found, otherwise null</returns>
    Task<UserPlatformFacebookSettings?> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves Facebook settings by their record ID
    /// </summary>
    /// <param name="id">The record ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The settings if found, otherwise null</returns>
    Task<UserPlatformFacebookSettings?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates Facebook settings for a user
    /// </summary>
    /// <param name="settings">The settings to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved settings if successful, otherwise null</returns>
    Task<UserPlatformFacebookSettings?> SaveAsync(UserPlatformFacebookSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes Facebook settings for a user
    /// </summary>
    /// <param name="id">The record ID</param>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found or an error occurred</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);
}

