using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Manager for user OAuth token operations
/// </summary>
public class UserOAuthTokenManager(IUserOAuthTokenDataStore dataStore) : IUserOAuthTokenManager
{
    /// <inheritdoc />
    public Task<UserOAuthToken?> GetByUserAndPlatformAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        return dataStore.GetByUserAndPlatformAsync(ownerOid, platformId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserOAuthToken?> StoreOAuthCallbackTokenAsync(
        string ownerOid,
        int platformId,
        string accessToken,
        string? refreshToken,
        DateTimeOffset accessTokenExpiresAt,
        DateTimeOffset? refreshTokenExpiresAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return dataStore.UpsertAsync(
            new UserOAuthToken
            {
                CreatedByEntraOid = ownerOid,
                SocialMediaPlatformId = platformId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        return dataStore.DeleteAsync(ownerOid, platformId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<UserOAuthToken>> GetExpiringWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return dataStore.GetExpiringWindowAsync(from, to, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UpdateLastNotifiedAtAsync(
        string ownerOid, int platformId, DateTimeOffset notifiedAt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        return dataStore.UpdateLastNotifiedAtAsync(ownerOid, platformId, notifiedAt, cancellationToken);
    }
}
