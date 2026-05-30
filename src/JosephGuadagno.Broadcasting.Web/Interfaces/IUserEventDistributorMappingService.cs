using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the current user's event distributor mappings API endpoints.
/// </summary>
public interface IUserEventDistributorMappingService
{
    /// <summary>
    /// Gets all event distributor mappings for the current user.
    /// </summary>
    Task<List<UserEventDistributorMapping>> GetAllAsync();

    /// <summary>
    /// Gets an event distributor mapping by identifier.
    /// </summary>
    Task<UserEventDistributorMapping?> GetAsync(int id);

    /// <summary>
    /// Creates an event distributor mapping for the current user.
    /// </summary>
    Task<UserEventDistributorMapping?> AddAsync(UserEventDistributorMapping mapping);

    /// <summary>
    /// Updates an existing event distributor mapping for the current user.
    /// </summary>
    Task<UserEventDistributorMapping?> UpdateAsync(UserEventDistributorMapping mapping);

    /// <summary>
    /// Deletes an event distributor mapping for the current user.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Toggles the IsActive status of an event distributor mapping.
    /// </summary>
    Task<bool> ToggleActiveAsync(int id);
}
