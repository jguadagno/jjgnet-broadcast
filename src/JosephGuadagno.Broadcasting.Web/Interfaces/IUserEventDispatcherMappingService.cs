using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the current user's event dispatcher mappings API endpoints.
/// </summary>
public interface IUserEventDispatcherMappingService
{
    /// <summary>
    /// Gets all event dispatcher mappings for the current user.
    /// </summary>
    Task<List<UserEventDispatcherMapping>> GetAllAsync();

    /// <summary>
    /// Gets an event dispatcher mapping by identifier.
    /// </summary>
    Task<UserEventDispatcherMapping?> GetAsync(int id);

    /// <summary>
    /// Creates an event dispatcher mapping for the current user.
    /// </summary>
    Task<UserEventDispatcherMapping?> AddAsync(UserEventDispatcherMapping mapping);

    /// <summary>
    /// Updates an existing event dispatcher mapping for the current user.
    /// </summary>
    Task<UserEventDispatcherMapping?> UpdateAsync(UserEventDispatcherMapping mapping);

    /// <summary>
    /// Deletes an event dispatcher mapping for the current user.
    /// </summary>
    Task<bool> DeleteAsync(int id);
}
