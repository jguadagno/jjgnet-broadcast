using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the current user's random post settings API endpoints.
/// </summary>
public interface IUserRandomPostSettingsService
{
    /// <summary>
    /// Gets all random post settings for the current user.
    /// </summary>
    Task<List<UserRandomPostSettings>> GetAllAsync();

    /// <summary>
    /// Gets a random post settings record by identifier.
    /// </summary>
    Task<UserRandomPostSettings?> GetAsync(int id);

    /// <summary>
    /// Creates a random post settings record for the current user.
    /// </summary>
    Task<UserRandomPostSettings?> AddAsync(UserRandomPostSettings settings);

    /// <summary>
    /// Updates an existing random post settings record for the current user.
    /// </summary>
    Task<UserRandomPostSettings?> UpdateAsync(UserRandomPostSettings settings);

    /// <summary>
    /// Deletes a random post settings record for the current user.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Toggles the IsActive status of a random post settings record.
    /// </summary>
    Task<bool> ToggleActiveAsync(int id);
}
