using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for application user operations
/// </summary>
public interface IApplicationUserDataStore
{
    /// <summary>
    /// Gets a user by their Microsoft Entra object ID
    /// </summary>
    /// <param name="entraObjectId">The Entra object ID</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<ApplicationUser?> GetByEntraObjectIdAsync(string entraObjectId);

    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<ApplicationUser?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>List of all users</returns>
    Task<List<ApplicationUser>> GetAllAsync();

    /// <summary>
    /// Gets all users with the specified approval status
    /// </summary>
    /// <param name="approvalStatus">The approval status to filter by</param>
    /// <returns>List of users with the specified status</returns>
    Task<List<ApplicationUser>> GetByApprovalStatusAsync(string approvalStatus);

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>The created user</returns>
    Task<ApplicationUser> CreateAsync(ApplicationUser user);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <returns>The updated user</returns>
    Task<ApplicationUser> UpdateAsync(ApplicationUser user);
}
