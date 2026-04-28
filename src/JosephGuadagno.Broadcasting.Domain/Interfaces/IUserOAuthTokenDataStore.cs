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

    /// <summary>
    /// Returns all tokens whose AccessTokenExpiresAt falls within the window [<paramref name="from"/>, <paramref name="to"/>].
    /// Used by notification Functions to find tokens expiring soon.
    /// </summary>
    /// <param name="from">Start of the expiry window (inclusive)</param>
    /// <param name="to">End of the expiry window (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tokens whose access token expires within the window</returns>
    Task<List<UserOAuthToken>> GetExpiringWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the LastNotifiedAt timestamp for a specific token.
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="platformId">The social media platform ID</param>
    /// <param name="notifiedAt">The timestamp when the notification was sent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated, false if the token was not found</returns>
    Task<bool> UpdateLastNotifiedAtAsync(
        string ownerOid, int platformId, DateTimeOffset notifiedAt, CancellationToken cancellationToken = default);
}
