using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the Social Media Platforms API
/// </summary>
public interface ISocialMediaPlatformService
{
    /// <summary>
    /// Gets all social media platforms including inactive ones (for admin use)
    /// </summary>
    Task<List<SocialMediaPlatform>> GetAllAsync();

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
