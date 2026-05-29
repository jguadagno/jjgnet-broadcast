using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the current user's event dispatcher mappings API endpoints.
/// </summary>
public interface IUserEventDistributorMappingService
{
    /// <summary>
    /// Gets all event dispatcher mappings for the current user.
    /// </summary>
    Task<List<UserEventDistributorMapping>> GetAllAsync();

    /// <summary>
    /// Gets an event dispatcher mapping by identifier.
    /// </summary>
    Task<UserEventDistributorMapping?> GetAsync(int id);

    /// <summary>
    /// Creates an event dispatcher mapping for the current user.
    /// </summary>
    Task<UserEventDistributorMapping?> AddAsync(UserEventDistributorMapping mapping);

    /// <summary>
    /// Updates an existing event dispatcher mapping for the current user.
    /// </summary>
    Task<UserEventDistributorMapping?> UpdateAsync(UserEventDistributorMapping mapping);

    /// <summary>
    /// Deletes an event dispatcher mapping for the current user.
    /// </summary>
    Task<bool> DeleteAsync(int id);
}
