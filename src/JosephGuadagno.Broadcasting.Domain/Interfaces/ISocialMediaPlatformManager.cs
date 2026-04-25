using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager interface for social media platform operations
/// </summary>
public interface ISocialMediaPlatformManager
{
    /// <summary>
    /// Get all social media platforms
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive platforms (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of social media platforms</returns>
    Task<List<SocialMediaPlatform>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all social media platforms including inactive ones (for admin use)
    /// </summary>
    /// <param name='cancellationToken'>Cancellation token</param>
    /// <returns>List of all social media platforms including inactive</returns>
    Task<List<SocialMediaPlatform>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a social media platform by ID
    /// </summary>
    /// <param name="id">The platform ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The social media platform or null if not found</returns>
    Task<SocialMediaPlatform?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a social media platform by name
    /// </summary>
    /// <param name="name">The platform name (e.g., "Twitter", "Bluesky")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The social media platform or null if not found</returns>
    Task<SocialMediaPlatform?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new social media platform
    /// </summary>
    /// <param name="platform">The platform to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added platform or null if failed</returns>
    Task<SocialMediaPlatform?> AddAsync(SocialMediaPlatform platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing social media platform
    /// </summary>
    /// <param name="platform">The platform to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated platform or null if failed</returns>
    Task<SocialMediaPlatform?> UpdateAsync(SocialMediaPlatform platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete (soft delete) a social media platform
    /// </summary>
    /// <param name="id">The platform ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<SocialMediaPlatform>> GetAllAsync(int page, int pageSize, string sortBy = "name", bool sortDescending = false, string? filter = null, bool includeInactive = false, CancellationToken cancellationToken = default);
}
