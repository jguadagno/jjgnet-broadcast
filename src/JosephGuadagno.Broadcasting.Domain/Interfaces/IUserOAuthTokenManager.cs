using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for OAuth token operations
/// </summary>
public interface IUserOAuthTokenManager
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
    /// Stores (create or replace) the token issued by an OAuth callback.
    /// </summary>
    /// <param name="ownerOid">The Entra Object ID of the user</param>
    /// <param name="platformId">The social media platform ID</param>
    /// <param name="accessToken">The OAuth access token</param>
    /// <param name="refreshToken">The OAuth refresh token if provided</param>
    /// <param name="accessTokenExpiresAt">When the access token expires</param>
    /// <param name="refreshTokenExpiresAt">When the refresh token expires if provided</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored token if successful, otherwise null</returns>
    Task<UserOAuthToken?> StoreOAuthCallbackTokenAsync(
        string ownerOid,
        int platformId,
        string accessToken,
        string? refreshToken,
        DateTimeOffset accessTokenExpiresAt,
        DateTimeOffset? refreshTokenExpiresAt,
        CancellationToken cancellationToken = default);

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
