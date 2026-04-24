using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for managing OAuth tokens for users and social media platforms
/// </summary>
public interface IUserOAuthTokenDataStore
{
    /// <summary>
    /// Retrieves the OAuth token for a specific user and platform
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="platformId">The social media platform ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The OAuth token if found, otherwise null</returns>
    Task<UserOAuthToken?> GetByUserAndPlatformAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates an OAuth token for a user and platform
    /// </summary>
    /// <param name="token">The token to upsert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The upserted token if successful, otherwise null</returns>
    Task<UserOAuthToken?> UpsertAsync(
        UserOAuthToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an OAuth token for a specific user and platform
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="platformId">The social media platform ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found or error occurred</returns>
    Task<bool> DeleteAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all tokens whose AccessTokenExpiresAt is at or before <paramref name="threshold"/>.
    /// Used by background refresh Functions.
    /// </summary>
    /// <param name="threshold">The expiry threshold date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tokens expiring at or before the threshold</returns>
    Task<List<UserOAuthToken>> GetExpiringAsync(
        DateTimeOffset threshold, CancellationToken cancellationToken = default);
}
