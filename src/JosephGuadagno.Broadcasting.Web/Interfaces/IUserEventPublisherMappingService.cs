using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the current user's event publisher mappings API endpoints.
/// </summary>
public interface IUserEventPublisherMappingService
{
    /// <summary>
    /// Gets all event publisher mappings for the current user.
    /// </summary>
    Task<List<UserEventPublisherMapping>> GetAllAsync();

    /// <summary>
    /// Gets an event publisher mapping by identifier.
    /// </summary>
    Task<UserEventPublisherMapping?> GetAsync(int id);

    /// <summary>
    /// Creates an event publisher mapping for the current user.
    /// </summary>
    Task<UserEventPublisherMapping?> AddAsync(UserEventPublisherMapping mapping);

    /// <summary>
    /// Updates an existing event publisher mapping for the current user.
    /// </summary>
    Task<UserEventPublisherMapping?> UpdateAsync(UserEventPublisherMapping mapping);

    /// <summary>
    /// Deletes an event publisher mapping for the current user.
    /// </summary>
    Task<bool> DeleteAsync(int id);
}
