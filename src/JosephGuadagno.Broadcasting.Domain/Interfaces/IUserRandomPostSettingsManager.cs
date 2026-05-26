using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user random post schedules and filtering settings.
/// </summary>
public interface IUserRandomPostSettingsManager
{
    /// <summary>
    /// Retrieves random post settings for a specific user.
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user.</param>
    /// <param name="activeOnly">When true, only returns active configurations; otherwise returns all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of random post settings for the user.</returns>
    Task<List<UserRandomPostSettings>> GetByUserAsync(string ownerOid, bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves random post settings by their record ID.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The settings if found, otherwise null.</returns>
    Task<UserRandomPostSettings?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active random post settings across all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active random post settings.</returns>
    Task<List<UserRandomPostSettings>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates random post settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved settings if successful, otherwise null.</returns>
    Task<UserRandomPostSettings?> SaveAsync(UserRandomPostSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes random post settings for a user.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="ownerOid">The Entra Object ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found or an error occurred.</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);
}
