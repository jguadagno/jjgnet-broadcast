using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for managing per-user random post schedules and filtering settings.
/// </summary>
public interface IUserRandomPostSettingsDataStore
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
    /// Retrieves all active random post settings that are due to run at or before <paramref name="utcNow"/>.
    /// A record is due when <c>NextRunDateUtc IS NULL</c> (never run) or <c>NextRunDateUtc &lt;= utcNow</c>.
    /// </summary>
    /// <param name="utcNow">The current UTC instant to compare against <c>NextRunDateUtc</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of due, active random post settings.</returns>
    Task<List<UserRandomPostSettings>> GetAllDueAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the <c>NextRunDateUtc</c> field for a single random post settings record after a successful dispatch.
    /// Resets <c>CronParseFailureCount</c> to 0 when the record is successfully updated.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="nextRunUtc">The next scheduled UTC run time, or null when no future occurrence exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the record was found and updated; false otherwise.</returns>
    Task<bool> UpdateNextRunAsync(int id, DateTimeOffset? nextRunUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the consecutive cron parse failure count for a random post settings record.
    /// If the count reaches or exceeds <paramref name="failureThreshold"/>, the record is deactivated
    /// (IsActive set to false) and the method returns true to signal deactivation.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="failureThreshold">The consecutive failure count at which deactivation occurs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the record was deactivated; false if merely incremented or not found.</returns>
    Task<bool> IncrementCronFailureAsync(int id, int failureThreshold = 5, CancellationToken cancellationToken = default);

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
