using System;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class UserOAuthTokenManager(IUserOAuthTokenDataStore dataStore) : IUserOAuthTokenManager
{
    public Task<UserOAuthToken?> GetByUserAndPlatformAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        return dataStore.GetByUserAndPlatformAsync(ownerOid, platformId, cancellationToken);
    }

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

    public Task<bool> DeleteAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        return dataStore.DeleteAsync(ownerOid, platformId, cancellationToken);
    }
}
