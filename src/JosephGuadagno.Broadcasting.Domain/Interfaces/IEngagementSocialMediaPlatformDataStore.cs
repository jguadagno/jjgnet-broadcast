using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store interface for engagement-social media platform junction operations
/// </summary>
public interface IEngagementSocialMediaPlatformDataStore
{
    /// <summary>
    /// Get all social media platforms for an engagement
    /// </summary>
    /// <param name="engagementId">The engagement ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of engagement-platform associations</returns>
    Task<List<EngagementSocialMediaPlatform>> GetByEngagementIdAsync(int engagementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific social media platform association for an engagement
    /// </summary>
    /// <param name="engagementId">The engagement ID</param>
    /// <param name="platformId">The social media platform ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The engagement-platform association or null if not found</returns>
    Task<EngagementSocialMediaPlatform?> GetAsync(int engagementId, int platformId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a social media platform to an engagement
    /// </summary>
    /// <param name="engagementSocialMediaPlatform">The association to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added association or null if the save operation produced no changes</returns>
    /// <exception cref="JosephGuadagno.Broadcasting.Domain.Exceptions.DuplicateEngagementSocialMediaPlatformException">
    /// Thrown when the engagement already has the requested social media platform association.
    /// </exception>
    Task<EngagementSocialMediaPlatform?> AddAsync(EngagementSocialMediaPlatform engagementSocialMediaPlatform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a social media platform from an engagement
    /// </summary>
    /// <param name="engagementId">The engagement ID</param>
    /// <param name="socialMediaPlatformId">The platform ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(int engagementId, int socialMediaPlatformId, CancellationToken cancellationToken = default);
}
