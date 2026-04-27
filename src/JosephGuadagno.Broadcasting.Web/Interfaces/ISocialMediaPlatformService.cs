using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the Social Media Platforms API
/// </summary>
public interface ISocialMediaPlatformService
{
    /// <summary>
    /// Gets a paged list of social media platforms
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDescending">Sort direction</param>
    /// <param name="filter">Optional name filter</param>
    /// <param name="includeInactive">Whether to include inactive platforms</param>
    Task<PagedResult<SocialMediaPlatform>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null, bool includeInactive = false);

    /// <summary>
    /// Gets a social media platform by its ID
    /// </summary>
    Task<SocialMediaPlatform?> GetByIdAsync(int id);

    /// <summary>
    /// Adds a new social media platform
    /// </summary>
    Task<SocialMediaPlatform?> AddAsync(SocialMediaPlatform platform);

    /// <summary>
    /// Updates an existing social media platform
    /// </summary>
    Task<SocialMediaPlatform?> UpdateAsync(SocialMediaPlatform platform);

    /// <summary>
    /// Toggles the IsActive status of a social media platform (soft delete / reactivate)
    /// </summary>
    Task<bool> ToggleActiveAsync(int id);

    /// <summary>
    /// Deletes a social media platform via the API
    /// </summary>
    Task<bool> DeleteAsync(int id);
}
