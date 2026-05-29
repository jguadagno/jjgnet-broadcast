using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for managing per-user event-to-dispatcher mappings.
/// </summary>
public interface IUserEventDistributorMappingDataStore
{
    /// <summary>
    /// Retrieves event dispatcher mappings for a specific user.
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user.</param>
    /// <param name="activeOnly">When true, only returns active configurations; otherwise returns all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of mappings for the user.</returns>
    Task<List<UserEventDistributorMapping>> GetByUserAsync(string ownerOid, bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active event dispatcher mappings for a specific user and event type.
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user.</param>
    /// <param name="eventType">The event type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active mappings for the requested event type.</returns>
    Task<List<UserEventDistributorMapping>> GetByUserAndEventTypeAsync(string ownerOid, string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves event dispatcher mappings by their record ID.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping if found, otherwise null.</returns>
    Task<UserEventDistributorMapping?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates an event dispatcher mapping.
    /// </summary>
    /// <param name="mapping">The mapping to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved mapping if successful, otherwise null.</returns>
    Task<UserEventDistributorMapping?> SaveAsync(UserEventDistributorMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event dispatcher mapping for a user.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="ownerOid">The Entra Object ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found or an error occurred.</returns>
    Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default);
}
